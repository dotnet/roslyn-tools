// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        // Currently this is the only release we trigger. We can easily move this over to options when we need this configurable.
        private const string ReleaseDefinitionName = "Roslyn";

        private static readonly Lazy<TfsTeamProjectCollection> LazyProjectCollection = new Lazy<Microsoft.TeamFoundation.Client.TfsTeamProjectCollection>(() =>
        {
            Log.Trace($"Creating TfsTeamProjectCollection object from {Options.VSTSUri}");
            return new TfsTeamProjectCollection(new Uri(Options.VSTSUri), new VssBasicCredential(Options.Username, Options.Password));
        });

        private static TfsTeamProjectCollection ProjectCollection => LazyProjectCollection.Value;

        private static GitPullRequest CreatePullRequest(string sourceBranch, string targetBranch, string description)
        {
            Log.Trace($"Creating pull request sourceBranch:{sourceBranch} targetBranch:{targetBranch} description:{description}");
            return new GitPullRequest
            {
                Title = $"{Options.InsertionName} Insertion Into {Options.VisualStudioBranchName} ",
                Description = description,
                SourceRefName = sourceBranch,
                TargetRefName = targetBranch
            };
        }

        private static async Task<GitPullRequest> CreatePullRequestAsync(string branchName, string message, CancellationToken cancellationToken)
        {
            var gitClient = ProjectCollection.GetClient<GitHttpClient>();
            Log.Trace($"Getting remote repository from {Options.VisualStudioBranchName} in {Options.TFSProjectName}");
            var repository = await gitClient.GetRepositoryAsync(project: Options.TFSProjectName, repositoryId: "VS", cancellationToken: cancellationToken);
            return await gitClient.CreatePullRequestAsync(
                    CreatePullRequest("refs/heads/" + branchName, "refs/heads/" + Options.VisualStudioBranchName, message),
                    repository.Id,
                    supportsIterations: null,
                    userState: null,
                    cancellationToken);
        }

        private static async Task<IEnumerable<Build>> GetBuildsFromTFSAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, CancellationToken cancellationToken, BuildResult? resultFilter = default(BuildResult))
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

            return (await BuildsWithValidArtifactsAsync(buildClient, cancellationToken, from build in builds
                    orderby build.FinishTime descending
                    select build)).FirstOrDefault();
        }

        private static async Task<List<Build>> BuildsWithValidArtifactsAsync(BuildHttpClient buildClient, CancellationToken cancellationToken,
                        IEnumerable<Build> builds)
        {
            List<Build> buildsWithValidArtifacts = new List<Build>();
            foreach (var build in builds)
            {
                var artifacts = await buildClient.GetArtifactsAsync(build.Project.Id, build.Id, cancellationToken);
                if (artifacts.Any(a => a.Resource != null && a.Resource.Data != null && a.Resource.Data.Contains(Options.BuildDropPath)))
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
            Log.Info($"Get Latest Passed Build");
            try
            {
                Log.Trace($"Getting latest passing build from {Options.TFSProjectName} where name is {Options.BuildQueueName}");
                var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
                var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.BuildQueueName);
                var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken, BuildResult.Succeeded);

                // Get the latest build with valid artifacts.
                newestBuild = (await BuildsWithValidArtifactsAsync(buildClient, cancellationToken,
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
                    Log.Info($"Started '{buildPolicy}' build policy on {pullRequest.Description}");
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
            Log.Trace($"Getting latest passing build from {Options.TFSProjectName} where name is {Options.BuildQueueName}");
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.BuildQueueName);
            var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken);
            return (from build in builds
                    where version == BuildVersion.FromTfsBuildNumber(build.BuildNumber, Options.BuildQueueName)
                    orderby build.FinishTime descending
                    select build).FirstOrDefault();
        }

        private static async Task<Release> CreateReleaseAsync(Build build, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var releaseClient = ProjectCollection.GetClient<ReleaseHttpClient>();

            var releaseDefinition = (await releaseClient.GetReleaseDefinitionsAsync(Options.TFSProjectName, ReleaseDefinitionName, cancellationToken: cancellationToken)).FirstOrDefault();

            if (releaseDefinition == null)
            {
                Log.Error($"Could not find a release definition with name: {ReleaseDefinitionName}");
                return null;
            }

            var releaseMetadata = new ReleaseStartMetadata()
            {
                DefinitionId = releaseDefinition.Id,
                Description = $"Automated release for {Options.BranchName} for build {build.BuildNumber}",
                Reason = ReleaseReason.ContinuousIntegration
            };

            var artifactMetadata = new ArtifactMetadata
            {
                Alias = "Roslyn-Signed", // This is the alias in the Release definition.
                InstanceReference = new Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts.BuildVersion
                {
                    Id = build.Id.ToString(CultureInfo.InvariantCulture),
                    Name = build.BuildNumber
                }
            };

            artifactMetadata.InstanceReference.SourceBranch = build.SourceBranch;
            releaseMetadata.Artifacts = new List<ArtifactMetadata> { artifactMetadata };

            return await releaseClient.CreateReleaseAsync(releaseMetadata, Options.TFSProjectName, cancellationToken: cancellationToken);
        }

        // Apparently there isn't a very nice way of waiting for a release to complete. Borrowed this piece from an internal test code.
        private static void WaitForReleaseCompletion(Release release, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (release == null)
            {
                throw new ArgumentNullException(nameof(release));
            }

            try
            {
                foreach (var env in release.Environments)
                {
                    WaitForReleaseEnvironmentCompletion(Options.TFSProjectName, release, env.Id, timeout, cancellationToken);
                }
            }
            catch(Exception exception)
            {
                // Log and swallow exceptions here.
                // These aren't as severe as to stop creating an insertion PR.
                Log.Error(exception);
            }
        }

        private static EnvironmentStatus WaitForReleaseEnvironmentCompletion(string projectName, Release release, int environmentId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (release == null)
            {
                throw new ArgumentNullException(nameof(release));
            }

            Log.Info("Waiting for release environment to complete. releaseId: {0}, environmentId: {1} in project: {2}", release.Id, environmentId, projectName);

            EnvironmentStatus environmentStatus;
            ReleaseEnvironment releaseEnvironment;

            var releaseClient = ProjectCollection.GetClient<ReleaseHttpClient>();

            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Task.Delay(5 * 1000, cancellationToken).Wait();

                    environmentStatus = GetReleaseEnvironment(projectName, release.Id, environmentId, releaseClient, cancellationToken).Status;

                    if(!(environmentStatus.Equals(EnvironmentStatus.InProgress)
                          || environmentStatus.Equals(EnvironmentStatus.Queued)
                          || environmentStatus.Equals(EnvironmentStatus.NotStarted)
                          || environmentStatus.Equals(EnvironmentStatus.Scheduled)))
                    {
                        break;
                    }

                    if (watch.Elapsed > timeout)
                    {
                        throw new TimeoutException($"The release could not be completed within {timeout.Minutes} minutes");
                    }
                }
            }
            finally
            {
                releaseEnvironment = GetReleaseEnvironment(projectName, release.Id, environmentId, releaseClient, cancellationToken);

                if (releaseEnvironment.Status != EnvironmentStatus.Succeeded)
                {
                    Log.Error($"The release did not succeed. Take a look at {release.ReleaseDefinitionReference.Url} to find {release.Name} for more details.");
                }
            }

            return releaseEnvironment.Status;
        }

        private static ReleaseEnvironment GetReleaseEnvironment(string projectName, int releaseId, int releaseEnvironmentId, ReleaseHttpClient releaseClient, CancellationToken cancellationToken)
        {
            Log.Info("GetReleaseEnvironment: Getting release environment. environmentId: {0}, releaseId:{1}", releaseEnvironmentId, releaseId);

            return releaseClient.GetReleaseEnvironmentAsync(projectName, releaseId, releaseEnvironmentId, cancellationToken: cancellationToken).SyncResult();
        }

        private static async Task<Component[]> GetLatestComponentsAsync(Build newestBuild, CancellationToken cancellationToken)
        {
            var logText = await GetLogTextAsync(newestBuild, cancellationToken);
            var urls = GetUrls(logText);
            return GetComponents(urls);
        }

        private static Component[] GetComponents(string[] urls)
        {
            if (urls == null || urls.Length == 0)
            {
                Log.Warn("GetComponents: No URLs specified.");
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
                    Log.Error(ex, $"Exception thrown creating Uri from {urlString}");
                    throw;
                }

                var fileName = urlString.Split(';').Last();
                var name = fileName.Remove(fileName.Length - 6, 6);
                result[i] = new Component(name, fileName, uri);
            }

            return result;
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
                Log.Info($"Manifest URL: {url}");
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
                    Log.Trace($"Listing all log file entries:{Environment.NewLine}{zipFileEntries}");
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
    }
}
