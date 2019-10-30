// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static readonly Lazy<TfsTeamProjectCollection> LazyProjectCollection = new Lazy<TfsTeamProjectCollection>(() =>
        {
            Console.WriteLine($"Creating TfsTeamProjectCollection object from {Options.VSTSUri}");
            return new TfsTeamProjectCollection(new Uri(Options.VSTSUri), new VssBasicCredential(Options.Username, Options.Password));
        });

        private static TfsTeamProjectCollection ProjectCollection => LazyProjectCollection.Value;

        private static GitPullRequest CreatePullRequest(string sourceBranch, string targetBranch, string description, string buildToInsert, string titlePrefix)
        {
            Console.WriteLine($"Creating pull request sourceBranch:{sourceBranch} targetBranch:{targetBranch} description:{description}");
            var prefix = string.IsNullOrEmpty(titlePrefix)
                ? string.Empty
                : titlePrefix + " ";
            return new GitPullRequest
            {
                Title = $"{prefix}{Options.InsertionName} '{Options.BranchName}/{buildToInsert}' Insertion into {Options.VisualStudioBranchName}",
                Description = description,
                SourceRefName = sourceBranch,
                TargetRefName = targetBranch,
                IsDraft = Options.CreateDraftPr,
            };
        }

        private static async Task<GitPullRequest> CreatePullRequestAsync(string branchName, string message, string buildToInsert, string titlePrefix, CancellationToken cancellationToken)
        {
            var gitClient = ProjectCollection.GetClient<GitHttpClient>();
            Console.WriteLine($"Getting remote repository from {Options.VisualStudioBranchName} in {Options.TFSProjectName}");
            var repository = await gitClient.GetRepositoryAsync(project: Options.TFSProjectName, repositoryId: "VS", cancellationToken: cancellationToken);
            return await gitClient.CreatePullRequestAsync(
                    CreatePullRequest("refs/heads/" + branchName, "refs/heads/" + Options.VisualStudioBranchName, message, buildToInsert, titlePrefix),
                    repository.Id,
                    supportsIterations: null,
                    userState: null,
                    cancellationToken);
        }

        private static async Task<IEnumerable<Build>> GetBuildsFromTFSAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, CancellationToken cancellationToken, BuildResult? resultFilter = null)
        {
            IEnumerable<Build> builds = await GetBuildsFromTFSByBranchAsync(buildClient, definitions, Options.BranchName, resultFilter, cancellationToken);
            builds = builds.Concat(await GetBuildsFromTFSByBranchAsync(buildClient, definitions, "refs/heads/" + Options.BranchName, resultFilter, cancellationToken));
            return builds;
        }

        private static async Task<List<Build>> GetBuildsFromTFSByBranchAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, string branchName, BuildResult? resultFilter, CancellationToken cancellationToken)
        {
            return await buildClient.GetBuildsAsync(
                project: Options.TFSProjectName,
                definitions: definitions.Select(d => d.Id),
                branchName: branchName,
                statusFilter: BuildStatus.Completed,
                resultFilter: resultFilter,
                cancellationToken: cancellationToken);
        }

        private static async Task<Build> GetLatestBuildAsync(CancellationToken cancellationToken)
        {
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.BuildQueueName);
            var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken, resultFilter: null);

            return (await GetInsertableBuildsAsync(buildClient, cancellationToken, from build in builds
                    orderby build.FinishTime descending
                    select build)).FirstOrDefault();
        }

        /// <summary>
        /// Insertable builds have valid artifacts and are not marked as 'DoesNotRequireInsertion_[TargetBranchName]'.
        /// </summary>
        private static async Task<List<Build>> GetInsertableBuildsAsync(
            BuildHttpClient buildClient,
            CancellationToken cancellationToken,
            IEnumerable<Build> builds)
        {
            List<Build> buildsWithValidArtifacts = new List<Build>();
            foreach (var build in builds)
            {
                if (build.Tags?.Contains($"DoesNotRequireInsertion_{Options.VisualStudioBranchName}") == true)
                {
                    continue;
                }

                // The artifact name passed to PublishBuildArtifacts task:
                var arcadeArtifactName = ArcadeInsertionArtifacts.ArtifactName;
                var legacyArtifactName = LegacyInsertionArtifacts.GetArtifactName(build.BuildNumber);

                var artifacts = await buildClient.GetArtifactsAsync(build.Project.Id, build.Id, cancellationToken);
                if (artifacts.Any(a => a.Name == arcadeArtifactName || a.Name == legacyArtifactName))
                {
                    buildsWithValidArtifacts.Add(build);
                }
            }
            return buildsWithValidArtifacts;
        }

        private static async Task<Build> GetLatestPassedBuildAsync(CancellationToken cancellationToken)
        {
            // ********************* Verify Build Passed *****************************
            cancellationToken.ThrowIfCancellationRequested();
            Build newestBuild = null;
            Console.WriteLine($"Get Latest Passed Build");
            try
            {
                Console.WriteLine($"Getting latest passing build for project {Options.TFSProjectName}, queue {Options.BuildQueueName}, and branch {Options.BranchName}");
                var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
                var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.BuildQueueName);
                var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken, BuildResult.Succeeded);

                // Get the latest build with valid artifacts.
                newestBuild = (await GetInsertableBuildsAsync(buildClient, cancellationToken,
                                    from build in builds
                                    orderby build.FinishTime descending
                                    select build)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new IOException($"Unable to get latest build for '{Options.BuildQueueName}' from project '{Options.TFSProjectName}' in '{Options.VSTSUri}': {ex.Message}");
            }

            if (newestBuild == null)
            {
                throw new IOException($"Unable to get latest build for '{Options.BuildQueueName}' from project '{Options.TFSProjectName}' in '{Options.VSTSUri}'");
            }

            // ********************* Get New Build Version****************************
            cancellationToken.ThrowIfCancellationRequested();
            return newestBuild;
        }

        // Similar to: https://devdiv.visualstudio.com/DevDiv/_git/PostBuildSteps#path=%2Fsrc%2FSubmitPullRequest%2FProgram.cs&version=GBmaster&_a=contents
        private static async Task QueueBuildPolicy(GitPullRequest pullRequest, string buildPolicy)
        {
            var policyClient = ProjectCollection.GetClient<PolicyHttpClient>();
            var repository = pullRequest.Repository;
            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var evaluations = await policyClient.GetPolicyEvaluationsAsync(repository.ProjectReference.Id, $"vstfs:///CodeReview/CodeReviewId/{repository.ProjectReference.Id}/{pullRequest.PullRequestId}");
                var evaluation = evaluations.FirstOrDefault(x =>
                {
                    if (x.Configuration.Type.DisplayName.Equals("Build", StringComparison.OrdinalIgnoreCase))
                    {
                        var policyName = x.Configuration.Settings["displayName"];
                        if (policyName != null)
                        {
                            return policyName.ToString().Equals(buildPolicy, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    return false;
                });

                if (evaluation != null)
                {
                    await policyClient.RequeuePolicyEvaluationAsync(repository.ProjectReference.Id, evaluation.EvaluationId);
                    Console.WriteLine($"Started '{buildPolicy}' build policy on {pullRequest.Description}");
                    break;
                }

                if (stopwatch.Elapsed > timeout)
                {
                    throw new ArgumentException($"Cannot find a '{buildPolicy}' build policy in {pullRequest.Description}.");
                }
            }
        }

        private static async Task<Build> GetSpecificBuildAsync(BuildVersion version, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Getting latest passing build from {Options.TFSProjectName} where name is {Options.BuildQueueName}");
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.BuildQueueName);
            var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken);
            return (from build in builds
                    where version == BuildVersion.FromTfsBuildNumber(build.BuildNumber, Options.BuildQueueName)
                    orderby build.FinishTime descending
                    select build).FirstOrDefault();
        }

        internal static async Task<InsertionArtifacts> GetInsertionArtifactsAsync(Build build, CancellationToken cancellationToken)
        {
            // used for local testing:
            if (LegacyInsertionArtifacts.TryCreateFromLocalBuild(Options.BuildDropPath, out var artifacts) ||
                ArcadeInsertionArtifacts.TryCreateFromLocalBuild(Options.BuildDropPath, out artifacts))
            {
                return artifacts;
            }

            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();

            Debug.Assert(ReferenceEquals(build,
                (await GetInsertableBuildsAsync(buildClient, cancellationToken, new[] { build })).Single()));

            // Pull the VSSetup directory from artifacts store.
            var buildArtifacts = await buildClient.GetArtifactsAsync(build.Project.Id, build.Id, cancellationToken);

            // The artifact name passed to PublishBuildArtifacts task:
            var arcadeArtifactName = ArcadeInsertionArtifacts.ArtifactName;
            var legacyArtifactName = LegacyInsertionArtifacts.GetArtifactName(build.BuildNumber);

            foreach (var artifact in buildArtifacts)
            {
                if (artifact.Name == arcadeArtifactName)
                {
                    // artifact.Resource.Data should be available and non-null due to BuildWithValidArtifactsAsync,
                    // which checks this precondition
                    if (!StringComparer.OrdinalIgnoreCase.Equals(artifact.Resource.Type, "container"))
                    {
                        throw new InvalidOperationException($"Could not find artifact '{arcadeArtifactName}' associated with build '{build.Id}'");
                    }

                    return new ArcadeInsertionArtifacts(await DownloadArtifactsAsync(buildClient, build, artifact, cancellationToken));
                }
                else if (artifact.Name == legacyArtifactName)
                {
                    // artifact.Resource.Data should be available and non-null due to BuildWithValidArtifactsAsync,
                    // which checks this precondition
                    if (string.Compare(artifact.Resource.Type, "container", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // This is a build where the artifacts are published to the artifacts server instead of a UNC path.
                        // Download this artifacts to a temp folder and provide that path instead.
                        return new LegacyInsertionArtifacts(await DownloadArtifactsAsync(buildClient, build, artifact, cancellationToken));
                    }

                    return new LegacyInsertionArtifacts(Path.Combine(artifact.Resource.Data, build.BuildNumber));
                }
            }

            // Should never happen since we already filtered for containing valid paths
            throw new InvalidOperationException("Could not find drop path");
        }

        private static async Task<string> DownloadArtifactsAsync(BuildHttpClient buildClient, Build build, BuildArtifact artifact, CancellationToken cancellationToken)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), string.Concat(Options.InsertionName, Options.BranchName).Replace(" ", "_").Replace("/","_"));
            if (Directory.Exists(tempDirectory))
            {
                // Be judicious and clean up old artifacts so we do not eat up memory on the scheduler machine.
                Directory.Delete(tempDirectory, recursive: true);

                // Sometimes a creation of a directory races with deletion since at least in .net 4.6 deletion is not a blocking call.
                // Hence explictly waiting for the directory to be deleted before moving on.
                Stopwatch w = Stopwatch.StartNew();

                while (Directory.Exists(tempDirectory) && w.ElapsedMilliseconds < 20 * 1000) Thread.Sleep(100);
            }

            Directory.CreateDirectory(tempDirectory);

            var archiveDownloadPath = Path.Combine(tempDirectory, string.Concat(artifact.Name, ".zip"));
            Console.WriteLine($"Downloading artifacts to {archiveDownloadPath}");

            Stopwatch watch = Stopwatch.StartNew();

            using (Stream s = await buildClient.GetArtifactContentZipAsync(Options.TFSProjectName, build.Id, artifact.Name, cancellationToken))
            using (MemoryStream ms = new MemoryStream())
            {
                await s.CopyToAsync(ms);
                using (ZipArchive archive = new ZipArchive(ms))
                {
                    archive.ExtractToDirectory(tempDirectory);
                }
            }

            Console.WriteLine($"Artifact download took {watch.ElapsedMilliseconds/1000} seconds");

            return Path.Combine(tempDirectory, artifact.Name);
        }

        private static async Task<Component[]> GetLatestComponentsAsync(Build newestBuild, CancellationToken cancellationToken)
        {
            var logText = await GetLogTextAsync(newestBuild, cancellationToken);
            var urls = GetUrls(logText);
            var components = await GetComponents(urls);
            return components;
        }

        private static async Task<Component[]> GetComponents(string[] urls)
        {
            if (urls == null || urls.Length == 0)
            {
                Console.WriteLine("GetComponents: No URLs specified.");
                return Array.Empty<Component>();
            }

            var result = new Component[urls.Length];
            for (var i = 0; i < urls.Length; i++)
            {
                var urlString = urls[i];

                Uri uri;
                try
                {
                    uri = new Uri(urlString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception thrown creating Uri from {urlString}: {ex}");
                    throw;
                }

                var fileName = urlString.Split(';').Last();
                var name = fileName.Remove(fileName.Length - 6, 6);
                var version = await GetVersionFromComponentUrl(uri);
                result[i] = new Component(name, fileName, uri, version);
            }

            return result;
        }

        private static async Task<string> GetVersionFromComponentUrl(Uri uri)
        {
            using (var client = new System.Net.WebClient())
            {
                var manifestText = await client.DownloadStringTaskAsync(uri);
                using (var stringStream = new MemoryStream(Encoding.UTF8.GetBytes(manifestText)))
                using (var streamReader = new StreamReader(stringStream))
                using (var reader = new JsonTextReader(streamReader))
                {
                    var jsonDocument = (JObject)JToken.ReadFrom(reader);
                    var infoObject = (JObject)jsonDocument["info"];
                    var version = infoObject.Value<string>("buildVersion"); // might not be present
                    return version;
                }
            }
        }

        private static string[] GetUrls(string logText)
        {
            const string startingString = "Manifest Url(s):";
            var manifestStart = logText.IndexOf(startingString);
            if (manifestStart == -1)
            {
                throw new Exception($"Could not locate string '{startingString}'");
            }

            // We're looking for URLs in the form of:
            // https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/dotnet/roslyn/dev15-rc2/20161122.1;PortableFacades.vsman
            const string pattern = @"https://.*vsman\r?$";
            var regex = new Regex(pattern, RegexOptions.Multiline);
            var input = logText.Substring(manifestStart);
            var matches = regex.Matches(input);

            if (matches.Count == 0)
            {
                throw new Exception($"No URLs found.");
            }

            var urls = new string[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                urls[i] = matches[i].Value.Trim();
            }

            foreach (var url in urls)
            {
                Console.WriteLine($"Manifest URL: {url}");
            }

            return urls;
        }

        private static async Task<string> GetLogTextAsync(Build newestBuild, CancellationToken cancellationToken)
        {
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();

            // Allocate temporary files
            var tempZipFile = Path.GetTempFileName();
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // Get zip file from TFS and write to disk
            using (var logStream = await buildClient.GetBuildLogsZipAsync(Options.TFSProjectName, newestBuild.Id, cancellationToken: cancellationToken))
            using (var tempStream = File.OpenWrite(tempZipFile))
            {
                await logStream.CopyToAsync(tempStream);
            }

            // Open zip file and extract log entry on disk
            using (var zipFile = ZipFile.OpenRead(tempZipFile))
            {
                var entry = zipFile.Entries.SingleOrDefault(x => x.FullName.Contains("Upload VSTS Drop"));
                if(entry == null)
                {
                    var zipFileEntries = string.Join(Environment.NewLine, zipFile.Entries.Select(x => x.FullName));
                    Console.WriteLine($"Listing all log file entries:{Environment.NewLine}{zipFileEntries}");
                    throw new Exception("This build did not upload its contents to VSTS Drop and is invalid.");
                }
                entry.ExtractToFile(tempFile);
            }

            // Read in log text
            string logText;
            using (var tempFileStream = File.OpenRead(tempFile))
            using (var streamReader = new StreamReader(tempFileStream))
            {
                logText = await streamReader.ReadToEndAsync();
            }

            // Attempt to delete temporary files
            try
            {
                File.Delete(tempZipFile);
                File.Delete(tempFile);
            }
            catch (Exception)
            {
                // swallow exceptions
            }

            return logText;
        }

        internal static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsAsync(Build fromBuild, Build tobuild, CancellationToken cancellationToken)
        {

            if (tobuild.Repository.Type == "GitHub")
            {
                var repoId = tobuild.Repository.Id; // e.g. dotnet/roslyn

                var fromSHA = fromBuild.SourceVersion.Substring(0, 7);
                var toSHA = tobuild.SourceVersion.Substring(0, 7);

                var restEndpoint = $"https://api.github.com/repos/{repoId}/compare/{fromSHA}...{toSHA}";
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, restEndpoint);
                request.Headers.Add("User-Agent", "RoslynInsertionTool");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                // https://developer.github.com/v3/repos/commits/
                var data = JsonConvert.DeserializeAnonymousType(content, new
                {
                    commits = new[]
                    {
                        new
                        {
                            sha = "",
                            commit = new
                            {
                                author = new
                                {
                                    name = "",
                                    email = "",
                                    date = ""
                                },
                                message = ""
                            },
                            html_url = ""
                        }
                    }
                });

                var result = data.commits
                    .Select(d =>
                        new GitCommit()
                        {
                            Author = d.commit.author.name,
                            CommitDate = DateTime.Parse(d.commit.author.date),
                            Message = d.commit.message,
                            CommitId = d.sha,
                            RemoteUrl = d.html_url
                        })
                    // show HEAD first, base last
                    .Reverse()
                    .ToList();

                return (result, $"https://github.com/{repoId}/compare/{fromSHA}...{toSHA}?w=1");
            }

            throw new NotSupportedException("Only builds created from GitHub repos support enumerating commits.");
        }

        private static readonly Regex IsMergePRCommit = new Regex(@"^Merge pull request #(\d+) from");
        private static readonly Regex IsSquashedPRCommit = new Regex(@"\(#(\d+)\)$");

        internal static string AppendChangesToDescription(string prDescription, Build oldBuild, List<GitCommit> changes)
        {
            if (!changes.Any())
            {
                return prDescription;
            }

            var description = new StringBuilder(prDescription + Environment.NewLine);

            var firstCommit = changes[0];
            var repoURL = $"http://github.com/{oldBuild.Repository.Id}";
            description.AppendLine($@"Changes since [{oldBuild.SourceVersion.Substring(0, 7)}]({firstCommit.RemoteUrl})");

            foreach (var commit in changes)
            {
                // Exclude auto-merges
                if (commit.Author == "dotnet-automerge-bot")
                {
                    continue;
                }

                var match = IsMergePRCommit.Match(commit.Message);

                if (!match.Success)
                {
                    match = IsSquashedPRCommit.Match(commit.Message);
                }

                if (!match.Success)
                {
                    continue;
                }

                var prNumber = match.Groups[1].Value;

                var firstLine = commit.Message.Split('\n')[0];
                var prLink = $@"- [{firstLine}]({repoURL}/pull/{prNumber})";

                description.AppendLine(prLink);

                if (description.Length > 3500)
                {
                    description.AppendLine("Changelog truncated due to description length limit.");
                    break;
                }
            }

            return description.ToString();
        }

        internal static string AppendDiffToDescription(string prDescription, string diffLink)
        {
            var diff = $"[View Complete Diff of Changes]({diffLink})";
            var description = new StringBuilder(prDescription + Environment.NewLine);
            description.AppendLine("---");
            description.AppendLine(diff);
            return description.Length >= 3500 ? prDescription : description.ToString();
        }

        internal struct GitCommit
        {
            public string Author { get; set; }
            public DateTime CommitDate { get; set; }
            public string Message { get; set; }
            public string CommitId { get; set; }
            public string RemoteUrl { get; set; }
        }
    }
}
