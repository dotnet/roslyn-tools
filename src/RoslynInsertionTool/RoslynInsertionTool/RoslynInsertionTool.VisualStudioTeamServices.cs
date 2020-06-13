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

        private static GitPullRequest CreatePullRequest(string sourceBranch, string targetBranch, string description, string buildToInsert, string titlePrefix, string reviewerId)
        {
            Console.WriteLine($"Creating pull request sourceBranch:{sourceBranch} targetBranch:{targetBranch} description:{description}");
            var prefix = string.IsNullOrEmpty(titlePrefix)
                ? string.Empty
                : titlePrefix + " ";

            return new GitPullRequest
            {
                Title = GetPullRequestTitle(buildToInsert, prefix),
                Description = description,
                SourceRefName = sourceBranch,
                TargetRefName = targetBranch,
                IsDraft = Options.CreateDraftPr,
                Reviewers = new[] { new IdentityRefWithVote { Id = reviewerId } }
            };
        }

        private static string GetPullRequestTitle(string buildToInsert, string prefix)
        {
            return $"{prefix}{Options.InsertionName} '{Options.BranchName}/{buildToInsert}' Insertion into {Options.VisualStudioBranchName}";
        }

        private static async Task<GitPullRequest> CreatePullRequestAsync(string branchName, string message, string buildToInsert, string titlePrefix, string reviewerId, CancellationToken cancellationToken)
        {
            var gitClient = ProjectCollection.GetClient<GitHttpClient>();
            Console.WriteLine($"Getting remote repository from {Options.VisualStudioBranchName} in {Options.TFSProjectName}");
            var repository = await gitClient.GetRepositoryAsync(project: Options.TFSProjectName, repositoryId: "VS", cancellationToken: cancellationToken);
            return await gitClient.CreatePullRequestAsync(
                    CreatePullRequest("refs/heads/" + branchName, "refs/heads/" + Options.VisualStudioBranchName, message, buildToInsert, titlePrefix, reviewerId),
                    repository.Id,
                    supportsIterations: null,
                    userState: null,
                    cancellationToken);
        }

        public static async Task<GitPullRequest> OverwritePullRequestAsync(int pullRequestId, string message, string buildToInsert, string titlePrefix, CancellationToken cancellationToken)
        {
            var gitClient = ProjectCollection.GetClient<GitHttpClient>();

            return await gitClient.UpdatePullRequestAsync(
                new GitPullRequest
                {
                    Title = GetPullRequestTitle(buildToInsert, titlePrefix),
                    Description = message,
                    IsDraft = Options.CreateDraftPr
                },
                VSRepoId,
                pullRequestId,
                cancellationToken: cancellationToken);
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

        private static async Task<Build> GetLatestBuildAsync(CancellationToken cancellationToken, BuildResult? resultFilter = null)
        {
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.BuildQueueName);
            var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken, resultFilter);

            return (await GetInsertableBuildsAsync(buildClient, cancellationToken,
                        from build in builds
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
                // Get the latest build with valid artifacts.
                newestBuild = await GetLatestBuildAsync(cancellationToken, BuildResult.Succeeded | BuildResult.PartiallySucceeded);

                if (newestBuild?.Result == BuildResult.PartiallySucceeded)
                {
                    LogWarning($"The latest build being used, {newestBuild.BuildNumber} has partially succeeded!");
                }
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

        private static async Task TryQueueBuildPolicy(GitPullRequest pullRequest, string buildPolicy, string insertionBranchName)
        {
            try
            {
                await QueueBuildPolicy(pullRequest, buildPolicy);
            }
            catch (Exception ex)
            {
                LogWarning($"Unable to start {buildPolicy} for '{insertionBranchName}'");
                LogWarning(ex);
            }
        }

        /// <summary>
        /// There is no enum or class in Microsoft.TeamFoundation.SourceControl.WebApi defined for vote values so made my own here.
        /// Values are documented at https://docs.microsoft.com/en-us/dotnet/api/microsoft.teamfoundation.sourcecontrol.webapi.identityrefwithvote.vote?view=azure-devops-dotnet.
        /// </summary>
        public enum Vote : short
        {
            Approved = 10,
            ApprovedWithComment = 5,
            NoResponse = 0,
            NotReady = -5,
            Rejected = -10
        }

        // Similar to: https://devdiv.visualstudio.com/DevDiv/_git/PostBuildSteps#path=%2Fsrc%2FSubmitPullRequest%2FProgram.cs&version=GBmaster&_a=contents
        private static async Task SetAutoCompleteAsync(GitPullRequest pullRequest, string commitMessage, CancellationToken cancellationToken)
        {
            var gitClient = ProjectCollection.GetClient<GitHttpClient>();
            var repository = pullRequest.Repository;
            try
            {
                var idRefWithVote = await gitClient.CreatePullRequestReviewerAsync(
                    new IdentityRefWithVote { Vote = (short)Vote.Approved },
                    repository.Id,
                    pullRequest.PullRequestId,
                    VSLSnapUserId.ToString(),
                    cancellationToken: cancellationToken
                    );
                Console.WriteLine($"Updated {pullRequest.Description} with AutoApprove");

                pullRequest = await gitClient.UpdatePullRequestAsync(
                    new GitPullRequest
                    {
                        AutoCompleteSetBy = idRefWithVote,
                        CompletionOptions = new GitPullRequestCompletionOptions
                        {
                            DeleteSourceBranch = true,
                            MergeCommitMessage = commitMessage,
                            SquashMerge = true,
                        }
                    },
                    repository.Id,
                    pullRequest.PullRequestId,
                    cancellationToken: cancellationToken
                    );
                Console.WriteLine($"Updated {pullRequest.Description} with AutoComplete");
            }
            catch (Exception e)
            {
                LogWarning($"Could not set AutoComplete: {e.GetType().Name} : {e.Message}");
                LogWarning(e);
            }
        }

        private static async Task<Build> GetSpecificBuildAsync(BuildVersion version, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Getting build with build number {version}");
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.BuildQueueName);
            var builds = await buildClient.GetBuildsAsync(
                project: Options.TFSProjectName,
                definitions: definitions.Select(d => d.Id),
                buildNumber: version.ToString(),
                statusFilter: BuildStatus.Completed,
                cancellationToken: cancellationToken);

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
            var tempDirectory = Path.Combine(Path.GetTempPath(), string.Concat(Options.InsertionName, Options.BranchName).Replace(" ", "_").Replace("/", "_"));
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

            var archiveDownloadPath = Path.Combine(tempDirectory, artifact.Name);
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

            Console.WriteLine($"Artifact download took {watch.ElapsedMilliseconds / 1000} seconds");

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

            var allLogs = await buildClient.GetBuildLogsAsync(Options.TFSProjectName, newestBuild.Id, cancellationToken: cancellationToken);
            foreach (var log in allLogs)
            {
                var headerLine = await buildClient.GetBuildLogLinesAsync(Options.TFSProjectName, newestBuild.Id, log.Id, startLine: 0, endLine: 1, cancellationToken: cancellationToken);
                if (headerLine[0].Contains("Upload VSTS Drop"))
                {
                    using var stream = await buildClient.GetBuildLogAsync(Options.TFSProjectName, newestBuild.Id, log.Id, cancellationToken: cancellationToken);
                    var logText = await new StreamReader(stream).ReadToEndAsync();
                    return logText;
                }
            }

            throw new Exception($"Build {newestBuild.BuildNumber} did not upload its contents to VSTS Drop and is invalid.");
        }

        internal static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsAsync(Build fromBuild, Build tobuild, CancellationToken cancellationToken)
        {
            if (tobuild.Repository.Type == "GitHub")
            {
                var repoId = tobuild.Repository.Id; // e.g. dotnet/roslyn

                var fromSHA = fromBuild.SourceVersion;
                var toSHA = tobuild.SourceVersion;

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
                                committer = new
                                {
                                    name = ""
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
                            Committer = d.commit.committer.name,
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

        private static readonly Regex IsReleaseFlowCommit = new Regex(@"^Merge pull request #\d+ from dotnet/merges/");
        private static readonly Regex IsMergePRCommit = new Regex(@"^Merge pull request #(\d+) from");
        private static readonly Regex IsSquashedPRCommit = new Regex(@"\(#(\d+)\)(?:\n|$)");

        internal static string AppendChangesToDescription(string prDescription, Build oldBuild, List<GitCommit> changes)
        {
            if (!changes.Any())
            {
                return prDescription;
            }

            var description = new StringBuilder(prDescription + Environment.NewLine);

            var repoURL = $"//github.com/{oldBuild.Repository.Id}";

            var commitHeaderAdded = false;
            var mergePRHeaderAdded = false;
            var mergePRFound = false;

            foreach (var commit in changes)
            {
                // Once we've found a Merge PR we can exclude commits not committed by GitHub since Merge and Squash commits are committed by GitHub
                if (commit.Committer != "GitHub" && mergePRFound)
                {
                    continue;
                }

                // Exclude arcade dependency updates
                if (commit.Author == "dotnet-maestro[bot]")
                {
                    mergePRFound = true;
                    continue;
                }

                // Exclude merge commits from auto code-flow PRs (e.g. merges/master-to-master-vs-deps)
                if (IsReleaseFlowCommit.Match(commit.Message).Success)
                {
                    mergePRFound = true;
                    continue;
                }

                string comment = string.Empty;
                string prNumber = string.Empty;

                var match = IsMergePRCommit.Match(commit.Message);
                if (match.Success)
                {
                    prNumber = match.Groups[1].Value;

                    // Merge PR Messages are in the form "Merge pull request #39526 from mavasani/GetValueUsageInfoAssert\n\nFix an assert in IOperationExtension.GetValueUsageInfo"
                    // Try and extract the 3rd line since it is the useful part of the message, otherwise take the first line.
                    var lines = commit.Message.Split('\n');
                    comment = lines.Length > 2
                        ? $"{lines[2]} ({prNumber})"
                        : lines[0];
                }
                else
                {
                    match = IsSquashedPRCommit.Match(commit.Message);
                    if (match.Success)
                    {
                        prNumber = match.Groups[1].Value;

                        // Squash PR Messages are in the form "Nullable annotate TypeCompilationState and MessageID (#39449)"
                        // Take the 1st line since it should be descriptive.
                        comment = commit.Message.Split('\n')[0];
                    }
                }

                // We will print commit comments until we find the first merge PR
                if (!match.Success && mergePRFound)
                {
                    continue;
                }

                string prLink;

                if (match.Success)
                {
                    if (commitHeaderAdded && !mergePRHeaderAdded)
                    {
                        mergePRHeaderAdded = true;
                        description.AppendLine("### Merged PRs:");
                    }

                    mergePRFound = true;

                    // Replace "#{prNumber}" with "{prNumber}" so that AzDO won't linkify it
                    comment = comment.Replace($"#{prNumber}", prNumber);

                    prLink = $@"- [{comment}]({GetGitHubPullRequestUrl(repoURL, prNumber)})";
                }
                else
                {
                    if (!commitHeaderAdded)
                    {
                        commitHeaderAdded = true;
                        description.AppendLine("### Commits since last PR:");
                    }

                    var fullSHA = commit.CommitId;
                    var shortSHA = fullSHA.Substring(0, 7);

                    // Take the 1st line since it should be descriptive.
                    comment = $"{commit.Message.Split('\n')[0]} ({shortSHA})";

                    prLink = $@"- [{comment}]({repoURL}/commit/{fullSHA})";
                }

                const string limitMessage = "Changelog truncated due to description length limit.";
                const int hardLimit = 4000; // Azure DevOps limitation

                // we want to be able to fit this PR link, as well as the limit message (plus line breaks) in case the next PR link doesn't fit
                int limit = hardLimit - (prLink.Length + 1) - (limitMessage.Length + 1);
                if (description.Length > limit)
                {
                    description.AppendLine(limitMessage);
                    break;
                }
                else
                {
                    description.AppendLine(prLink);
                }
            }

            return description.ToString();
        }

        public static string GetGitHubPullRequestUrl(string repoURL, string prNumber)
            => $"{repoURL}/pull/{prNumber}";

        internal struct GitCommit
        {
            public string Author { get; set; }
            public string Committer { get; set; }
            public DateTime CommitDate { get; set; }
            public string Message { get; set; }
            public string CommitId { get; set; }
            public string RemoteUrl { get; set; }
        }
    }
}
