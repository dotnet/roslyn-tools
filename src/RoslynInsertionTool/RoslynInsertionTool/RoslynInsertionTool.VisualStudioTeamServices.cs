// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Client.Reporting;
using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static readonly Lazy<TfsTeamProjectCollection> LazyVisualStudioRepoConnection = new(() =>
        {
            Console.WriteLine($"Creating VisualStudioRepoConnection object from {Options.VisualStudioRepoAzdoUri}");
            return new TfsTeamProjectCollection(new Uri(Options.VisualStudioRepoAzdoUri), new VssBasicCredential(Options.VisualStudioRepoAzdoUsername, Options.VisualStudioRepoAzdoPassword));
        });

        private static readonly Lazy<TfsTeamProjectCollection> LazyComponentBuildConnection = new(() =>
        {
            if (string.IsNullOrEmpty(Options.ComponentBuildAzdoUri))
            {
                Console.WriteLine($"Using the VisualStudioRepoConnection object as our ComponentBuildConnection");
                return LazyVisualStudioRepoConnection.Value;
            }

            Console.WriteLine($"Creating ComponentBuildConnection object from {Options.ComponentBuildAzdoUri}");
            return new TfsTeamProjectCollection(new Uri(Options.ComponentBuildAzdoUri), new VssBasicCredential(Options.ComponentBuildAzdoUsername, Options.ComponentBuildAzdoPassword));
        });

        /// <summary>
        /// Used to connect to the AzDO instance which contains the VS repo.
        /// </summary>
        private static TfsTeamProjectCollection VisualStudioRepoConnection => LazyVisualStudioRepoConnection.Value;

        /// <summary>
        /// Used to connect to the AzDO instance which contains the repo of the Component being inserted.
        /// </summary>
        private static TfsTeamProjectCollection ComponentBuildConnection => LazyComponentBuildConnection.Value;

        private static GitPullRequest CreatePullRequest(string sourceBranch, string targetBranch, string description, string buildToInsert, string reviewerId)
        {
            Console.WriteLine($"Creating pull request sourceBranch:{sourceBranch} targetBranch:{targetBranch} description:{description}");

            return new GitPullRequest
            {
                Title = GetPullRequestTitle(buildToInsert),
                Description = description,
                SourceRefName = sourceBranch,
                TargetRefName = targetBranch,
                IsDraft = Options.CreateDraftPr,
                Reviewers = !string.IsNullOrEmpty(reviewerId) ? new[] { new IdentityRefWithVote { Id = reviewerId } } : null
            };
        }

        private static string GetPullRequestTitle(string buildToInsert)
        {
            var prefix = string.IsNullOrEmpty(Options.TitlePrefix)
                ? string.Empty
                : Options.TitlePrefix + " ";
            var suffix = string.IsNullOrEmpty(Options.TitleSuffix)
                ? string.Empty
                : " " + Options.TitleSuffix;

            return $"{prefix}{Options.InsertionName} '{Options.ComponentBranchName}/{buildToInsert}' Insertion into {Options.VisualStudioBranchName}{suffix}";
        }

        private static async Task<GitPullRequest> CreateVSPullRequestAsync(string branchName, string message, string buildToInsert, string reviewerId, CancellationToken cancellationToken)
        {
            var gitClient = VisualStudioRepoConnection.GetClient<GitHttpClient>();
            Console.WriteLine($"Getting remote repository from {Options.VisualStudioBranchName} in {Options.VisualStudioRepoProjectName}");
            var repository = await gitClient.GetRepositoryAsync(project: Options.VisualStudioRepoProjectName, repositoryId: "VS", cancellationToken: cancellationToken);
            return await gitClient.CreatePullRequestAsync(
                    CreatePullRequest("refs/heads/" + branchName, "refs/heads/" + Options.VisualStudioBranchName, message, buildToInsert, reviewerId),
                    repository.Id,
                    supportsIterations: null,
                    userState: null,
                    cancellationToken);
        }

        public static async Task<GitPullRequest> OverwritePullRequestAsync(int pullRequestId, string message, string buildToInsert, CancellationToken cancellationToken)
        {
            var gitClient = VisualStudioRepoConnection.GetClient<GitHttpClient>();

            return await gitClient.UpdatePullRequestAsync(
                new GitPullRequest
                {
                    Title = GetPullRequestTitle(buildToInsert),
                    Description = message,
                    IsDraft = Options.CreateDraftPr
                },
                VSRepoId,
                pullRequestId,
                cancellationToken: cancellationToken);
        }

        public static async Task RetainComponentBuild(Build buildToInsert)
        {
            var buildClient = ComponentBuildConnection.GetClient<BuildHttpClient>();

            Console.WriteLine("Marking inserted build for retention.");
            buildToInsert.KeepForever = true;
            await buildClient.UpdateBuildAsync(buildToInsert);
        }

        private static async Task<IEnumerable<Build>> GetComponentBuildsAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, CancellationToken cancellationToken, BuildResult? resultFilter = null)
        {
            IEnumerable<Build> builds = await GetComponentBuildsByBranchAsync(buildClient, definitions, Options.ComponentBranchName, resultFilter, cancellationToken);
            builds = builds.Concat(await GetComponentBuildsByBranchAsync(buildClient, definitions, "refs/heads/" + Options.ComponentBranchName, resultFilter, cancellationToken));
            return builds;
        }

        private static async Task<List<Build>> GetComponentBuildsByBranchAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, string branchName, BuildResult? resultFilter, CancellationToken cancellationToken)
        {
            return await buildClient.GetBuildsAsync(
                project: Options.ComponentBuildProjectNameOrFallback,
                definitions: definitions.Select(d => d.Id),
                branchName: branchName,
                statusFilter: BuildStatus.Completed,
                resultFilter: resultFilter,
                cancellationToken: cancellationToken);
        }

        private static async Task<Build> GetLatestComponentBuildAsync(CancellationToken cancellationToken, BuildResult? resultFilter = null)
        {
            var buildClient = ComponentBuildConnection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.ComponentBuildProjectNameOrFallback, name: Options.ComponentBuildQueueName);
            var builds = await GetComponentBuildsAsync(buildClient, definitions, cancellationToken, resultFilter);

            return (await GetInsertableComponentBuildsAsync(buildClient, cancellationToken,
                        from build in builds
                        orderby build.FinishTime descending
                        select build)).FirstOrDefault();
        }

        /// <summary>
        /// Insertable builds have valid artifacts and are not marked as 'DoesNotRequireInsertion_[TargetBranchName]'.
        /// </summary>
        private static async Task<List<Build>> GetInsertableComponentBuildsAsync(
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

        private static async Task<Build> GetLatestPassedComponentBuildAsync(CancellationToken cancellationToken)
        {
            // ********************* Verify Build Passed *****************************
            cancellationToken.ThrowIfCancellationRequested();
            Build newestBuild = null;
            Console.WriteLine($"Get Latest Passed Component Build");
            try
            {
                Console.WriteLine($"Getting latest passing build for project {Options.ComponentBuildProjectNameOrFallback}, queue {Options.ComponentBuildQueueName}, and branch {Options.ComponentBranchName}");
                // Get the latest build with valid artifacts.
                newestBuild = await GetLatestComponentBuildAsync(cancellationToken, BuildResult.Succeeded | BuildResult.PartiallySucceeded);

                if (newestBuild?.Result == BuildResult.PartiallySucceeded)
                {
                    LogWarning($"The latest build being used, {newestBuild.BuildNumber} has partially succeeded!");
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Unable to get latest build for '{Options.ComponentBuildQueueName}' from project '{Options.ComponentBuildProjectNameOrFallback}' in '{Options.ComponentBuildAzdoUri}': {ex.Message}");
            }

            if (newestBuild == null)
            {
                throw new IOException($"Unable to get latest build for '{Options.ComponentBuildQueueName}' from project '{Options.ComponentBuildProjectNameOrFallback}' in '{Options.ComponentBuildAzdoUri}'");
            }

            // ********************* Get New Build Version****************************
            cancellationToken.ThrowIfCancellationRequested();
            return newestBuild;
        }

        // Similar to: https://devdiv.visualstudio.com/DevDiv/_git/PostBuildSteps#path=%2Fsrc%2FSubmitPullRequest%2FProgram.cs&version=GBmaster&_a=contents
        private static async Task QueueVSBuildPolicy(GitPullRequest pullRequest, string buildPolicy)
        {
            var policyClient = VisualStudioRepoConnection.GetClient<PolicyHttpClient>();
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

        private static async Task TryQueueVSBuildPolicy(GitPullRequest pullRequest, string buildPolicy, string insertionBranchName)
        {
            try
            {
                await QueueVSBuildPolicy(pullRequest, buildPolicy);
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
            var gitClient = VisualStudioRepoConnection.GetClient<GitHttpClient>();
            var repository = pullRequest.Repository;
            try
            {
                var idRefWithVote = await gitClient.CreatePullRequestReviewerAsync(
                    new IdentityRefWithVote { Vote = (short)Vote.Approved },
                    repository.Id,
                    pullRequest.PullRequestId,
                    DotNetBotUserId.ToString(),
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

        private static async Task<Build> GetSpecificComponentBuildAsync(BuildVersion version, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"Getting build with build number {version}");
            var buildClient = ComponentBuildConnection.GetClient<BuildHttpClient>();

            var definitions = await buildClient.GetDefinitionsAsync(project: Options.ComponentBuildProjectNameOrFallback, name: Options.ComponentBuildQueueName);
            var builds = await buildClient.GetBuildsAsync(
                project: Options.ComponentBuildProjectNameOrFallback,
                definitions: definitions.Select(d => d.Id),
                buildNumber: version.ToString(),
                cancellationToken: cancellationToken);

            return (from build in builds
                    where version == BuildVersion.FromTfsBuildNumber(build.BuildNumber, Options.ComponentBuildQueueName)
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

            var buildClient = ComponentBuildConnection.GetClient<BuildHttpClient>();

            Debug.Assert(ReferenceEquals(build,
                (await GetInsertableComponentBuildsAsync(buildClient, cancellationToken, new[] { build })).Single()));

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

                    return new ArcadeInsertionArtifacts(await DownloadBuildArtifactsAsync(buildClient, build, artifact, cancellationToken));
                }
                else if (artifact.Name == legacyArtifactName)
                {
                    // artifact.Resource.Data should be available and non-null due to BuildWithValidArtifactsAsync,
                    // which checks this precondition
                    if (string.Compare(artifact.Resource.Type, "container", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // This is a build where the artifacts are published to the artifacts server instead of a UNC path.
                        // Download this artifacts to a temp folder and provide that path instead.
                        return new LegacyInsertionArtifacts(await DownloadBuildArtifactsAsync(buildClient, build, artifact, cancellationToken));
                    }

                    return new LegacyInsertionArtifacts(Path.Combine(artifact.Resource.Data, build.BuildNumber));
                }
            }

            // Should never happen since we already filtered for containing valid paths
            throw new InvalidOperationException("Could not find drop path");
        }

        private static async Task<string> DownloadBuildArtifactsAsync(BuildHttpClient buildClient, Build build, BuildArtifact artifact, CancellationToken cancellationToken)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            var archiveDownloadPath = Path.Combine(tempDirectory, artifact.Name);
            Console.WriteLine($"Downloading artifacts to {archiveDownloadPath}");

            Stopwatch watch = Stopwatch.StartNew();

            using (Stream s = await buildClient.GetArtifactContentZipAsync(Options.ComponentBuildProjectNameOrFallback, build.Id, artifact.Name, cancellationToken))
            using (var ms = new MemoryStream())
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

        private static Component[] GetLatestBuildComponents(Build newestBuild, InsertionArtifacts buildArtifacts, CancellationToken cancellationToken)
        {
            return Directory.EnumerateFiles(buildArtifacts.RootDirectory, "*.vsman", SearchOption.AllDirectories)
                .Select(GetComponentFromManifestFile)
                .OfType<Component>()
                .ToArray();
        }

        private static Component GetComponentFromManifestFile(string filePath)
        {
            Console.WriteLine($"GetComponentFromManifestFile: Opening manifest from {filePath}.");
            var fileName = Path.GetFileName(filePath);
            var manifestJson = File.ReadAllText(filePath);

            var manifest = JsonConvert.DeserializeAnonymousType(manifestJson, new
            {
                info = new
                {
                    manifestName = "",
                    buildVersion = ""
                },
                packages = new[]
                {
                new
                {
                    payloads = new[]
                    {
                        new
                        {
                            url = ""
                        }
                    }
                }
            }
            });

            // Find the first package payload where the url is in the expected format `http://{drop url};{filename}`
            var payload = manifest?.packages
                .Where(package => package?.payloads is not null)
                .SelectMany(package => package.payloads)
                .FirstOrDefault(payload => payload?.url?.Contains(";") == true);

            if (payload is null)
            {
                Console.WriteLine($"GetComponentFromManifestFile: Manifest {filePath} did not contain an insertable component.");
                return null;
            }

            // Everything is uploaded to the same drop, so we can take the url of a package and generate the manifest url.
            var url = new Uri($"{payload.url.Split(';')[0]};{fileName}");
            return new Component(manifest.info.manifestName, fileName, url, manifest.info.buildVersion);
        }

        internal static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsAsync(Build fromBuild, Build tobuild, CancellationToken cancellationToken)
        {
            var repoId = !string.IsNullOrEmpty(Options.ComponentGitHubRepoName)
                ? Options.ComponentGitHubRepoName
                : tobuild.Repository.Id; // e.g. dotnet/roslyn when GitHub, 7b863b8d-8cc3-431d-b06b-7136cc32bbe6 when AzDO

            var fromSHA = fromBuild.SourceVersion;
            var toSHA = tobuild.SourceVersion;

            if (tobuild.Repository.Type == "GitHub" || !string.IsNullOrEmpty(Options.ComponentGitHubRepoName))
            {
                return await GetChangesBetweenBuildsFromGitHubAsync(repoId, fromSHA, toSHA);
            }
            else if (tobuild.Repository.Type == "TfsGit")
            {
                return await GetChangesBetweenBuildsFromAzDOAsync(tobuild, repoId, fromSHA, toSHA);
            }

            throw new NotSupportedException("Only builds created from GitHub & AzDO repos support enumerating commits.");
        }

        private static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsFromAzDOAsync(Build tobuild, string repoId, string fromSHA, string toSHA)
        {
            var gitClient = ComponentBuildConnection.GetClient<GitHttpClient>();
            var getCommits = (await gitClient.GetCommitsAsync(
                repoId,
                new GitQueryCommitsCriteria()
                {
                    ItemVersion = new GitVersionDescriptor() { Version = fromSHA, VersionType = GitVersionType.Commit },
                    CompareVersion = new GitVersionDescriptor() { Version = toSHA, VersionType = GitVersionType.Commit }
                }))
                // AzDO does not provide the full commit message, so we must query for each commit to provide better messages for PR merge commits.
                .Select(c => gitClient.GetCommitAsync(c.CommitId, repoId));
            var commits = (await Task.WhenAll(getCommits))
                .Select(c =>
                    new GitCommit()
                    {
                        Author = c.Author.Name,
                        Committer = c.Committer.Name,
                        CommitDate = c.Committer.Date,
                        Message = c.Comment,
                        CommitId = c.CommitId,
                        RemoteUrl = ((ReferenceLink)c.Links.Links["web"]).Href
                    })
                .ToList();

            // AzDO does not have a UI for comparing two commits. Instead generate the REST API call to retrieve commits between two SHAs.
            return (commits, $"{tobuild.Repository.Url.OriginalString.Replace("_git", "_apis/git/repositories")}/commits?searchCriteria.itemVersion.version={fromSHA}&searchCriteria.itemVersion.versionType=commit&searchCriteria.compareVersion.version={toSHA}&searchCriteria.compareVersion.versionType=commit");
        }

        private static async Task<(List<GitCommit> changes, string diffLink)> GetChangesBetweenBuildsFromGitHubAsync(string repoId, string fromSHA, string toSHA)
        {
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

            return (result, $"//github.com/{repoId}/compare/{fromSHA}...{toSHA}?w=1");
        }

        internal static string AppendChangesToDescription(string prDescription, Build oldBuild, List<GitCommit> changes)
        {
            const int hardLimit = 4000; // Azure DevOps limitation

            if (!changes.Any())
            {
                return prDescription;
            }

            var description = new StringBuilder(prDescription + Environment.NewLine);

            var repoId = !string.IsNullOrEmpty(Options.ComponentGitHubRepoName)
                ? Options.ComponentGitHubRepoName
                : oldBuild.Repository.Id; // e.g. dotnet/roslyn when GitHub, 7b863b8d-8cc3-431d-b06b-7136cc32bbe6 when AzDO

            if (oldBuild.Repository.Type == "GitHub" || !string.IsNullOrEmpty(Options.ComponentGitHubRepoName))
            {
                AppendGitHubChangesToDescription(changes, hardLimit, description, repoId);
            }
            else if (oldBuild.Repository.Type == "TfsGit")
            {
                AppendAzDOChangesToDescription(oldBuild, changes, hardLimit, description);
            }

            var result = description.ToString();
            if (result.Length > hardLimit)
            {
                LogWarning($"PR description is {result.Length} characters long, but the limit is {hardLimit}.");
                LogWarning(result);
            }

            return result;
        }

        private static readonly Regex IsAzDOReleaseFlowCommit = new Regex(@"^Merged PR \d+: Merging .* to ");
        private static readonly Regex IsAzDOMergePRCommit = new Regex(@"^Merged PR (\d+):");
        public static string GetAzDOPullRequestUrl(string repoURL, string prNumber)
            => $"{repoURL}/pullrequest/{prNumber}";

        private static void AppendAzDOChangesToDescription(Build oldBuild, List<GitCommit> changes, int hardLimit, StringBuilder description)
        {
            var repoURL = oldBuild.Repository.Url.AbsoluteUri;

            var commitHeaderAdded = false;
            var mergePRHeaderAdded = false;
            var mergePRFound = false;

            // This needs to be updated with heuristics for determining merge commits which represent PRs being merged.
            foreach (var commit in changes)
            {
                // Exclude arcade dependency updates
                if (commit.Author == "DotNet Bot")
                {
                    mergePRFound = true;
                    continue;
                }

                // Exclude OneLoc localization PRs
                if (commit.Author == "Project Collection Build Service (devdiv)")
                {
                    mergePRFound = true;
                    continue;
                }

                // Exclude merge commits from auto code-flow PRs (e.g. main to Dev17)
                if (IsAzDOReleaseFlowCommit.Match(commit.Message).Success)
                {
                    mergePRFound = true;
                    continue;
                }

                // Merge PR Messages are in the form "Merged PR 320820: Resolving encoding issue on test summary pane, using UTF8 now\n\nAdded a StreamWriterWrapper to resolve encoding issue"
                string comment = commit.Message.Split('\n')[0];
                string prNumber = string.Empty;

                var match = IsAzDOMergePRCommit.Match(commit.Message);
                if (match.Success)
                {
                    prNumber = match.Groups[1].Value;
                    mergePRFound = true;
                }
                else
                {
                    // Todo: Determine if there is a format for AzDO squash merges that preserves the PR #
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

                    prLink = $@"- [{comment}]({GetAzDOPullRequestUrl(repoURL, prNumber)})";
                }
                else
                {
                    if (!commitHeaderAdded)
                    {
                        commitHeaderAdded = true;
                        description.AppendLine("### Commits since last PR:");
                    }

                    var shortSHA = commit.CommitId.Substring(0, 7);
                    prLink = $@"- [{comment} ({shortSHA})]({commit.RemoteUrl})";
                }

                const string limitMessage = "Changelog truncated due to description length limit.";

                // we want to be able to fit this PR link, as well as the limit message (plus line breaks) in case the next PR link doesn't fit
                int limit = hardLimit - (prLink.Length + Environment.NewLine.Length) - (limitMessage.Length + Environment.NewLine.Length);
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
        }

        private static readonly Regex IsGitHubReleaseFlowCommit = new Regex(@"^Merge pull request #\d+ from dotnet/merges/");
        private static readonly Regex IsGitHubMergePRCommit = new Regex(@"^Merge pull request #(\d+) from");
        private static readonly Regex IsGitHubSquashedPRCommit = new Regex(@"\(#(\d+)\)(?:\n|$)");
        public static string GetGitHubPullRequestUrl(string repoURL, string prNumber)
            => $"{repoURL}/pull/{prNumber}";

        private static void AppendGitHubChangesToDescription(List<GitCommit> changes, int hardLimit, StringBuilder description, string repoId)
        {
            var repoURL = $"//github.com/{repoId}";

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

                // Exclude merge commits from auto code-flow PRs (e.g. merges/main-to-main-vs-deps)
                if (IsGitHubReleaseFlowCommit.Match(commit.Message).Success)
                {
                    mergePRFound = true;
                    continue;
                }

                string comment = string.Empty;
                string prNumber = string.Empty;

                var match = IsGitHubMergePRCommit.Match(commit.Message);
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
                    match = IsGitHubSquashedPRCommit.Match(commit.Message);
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

                // we want to be able to fit this PR link, as well as the limit message (plus line breaks) in case the next PR link doesn't fit
                int limit = hardLimit - (prLink.Length + Environment.NewLine.Length) - (limitMessage.Length + Environment.NewLine.Length);
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
        }

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
