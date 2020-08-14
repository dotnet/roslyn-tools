// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client.CommandLine;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Roslyn.Insertion
{
    public static partial class RoslynInsertionTool
    {
        private const string PRBuildTagPrefix = "PRNumber:";

        public static readonly Guid VSRepoId = new Guid("a290117c-5a8a-40f7-bc2c-f14dbe3acf6d");
        //Easiest way to get these GUIDs is to create a PR search in AzDo
        //You'll get something like https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequests?_a=active&createdBy=GUID-here
        public static readonly Guid MLInfraSwatUserId = new Guid("6c25b447-1d90-4840-8fde-d8b22cb8733e");
        public static readonly Guid VSLSnapUserId = new Guid("9f64bc2f-479b-429f-a665-fec80e130b1f");

        private static List<string> WarningMessages { get; } = new List<string>();

        private static RoslynInsertionToolOptions Options { get; set; }

        public static async Task<(bool success, int pullRequestId)> PerformInsertionAsync(
            RoslynInsertionToolOptions options,
            CancellationToken cancellationToken)
        {
            Options = options;
            Console.WriteLine($"{Environment.NewLine}New Insertion Into {Options.VisualStudioBranchName} Started{Environment.NewLine}");

            var newPackageFiles = new List<string>();

            try
            {
                Console.WriteLine($"Verifying given authentication for {Options.VSTSUri}");
                try
                {
                    ProjectCollection.Authenticate();
                }
                catch (Exception ex)
                {
                    LogError($"Could not authenticate with {Options.VSTSUri}");
                    LogError(ex);
                    return (false, 0);
                }

                Console.WriteLine($"Verification succeeded for {Options.VSTSUri}");

                // ********************** Create dummy PR *****************************
                if (Options.CreateDummyPr)
                {
                    GitPullRequest dummyPR;
                    try
                    {
                        dummyPR = await CreatePlaceholderBranchAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Unable to create placeholder PR for '{options.VisualStudioBranchName}'");
                        LogError(ex);
                        return (false, 0);
                    }

                    if (dummyPR == null)
                    {
                        LogError($"Unable to create placeholder PR for '{options.VisualStudioBranchName}'");
                        return (false, 0);
                    }

                    return (true, dummyPR.PullRequestId);
                }

                // ********************** Get Last Insertion *****************************
                cancellationToken.ThrowIfCancellationRequested();

                BuildVersion buildVersion;

                Build buildToInsert;
                Build latestBuild = null;
                bool retainBuild = false;

                // Get the version from DevOps Pipelines queue, e.g. Roslyn-Master-Signed-Release.
                if (string.IsNullOrEmpty(Options.SpecificBuild))
                {
                    buildToInsert = await GetLatestPassedBuildAsync(cancellationToken);
                    buildVersion = BuildVersion.FromTfsBuildNumber(buildToInsert.BuildNumber, Options.BuildQueueName);
                    Console.WriteLine("Found build number " + buildVersion);

                    //  Get the latest build, whether passed or failed.  If the buildToInsert has already been inserted but
                    //  there is a later failing build, then send an error
                    latestBuild = await GetLatestBuildAsync(cancellationToken);
                }
                else
                {
                    buildVersion = BuildVersion.FromString(Options.SpecificBuild);
                    buildToInsert = await GetSpecificBuildAsync(buildVersion, cancellationToken);
                }

                var insertionArtifacts = await GetInsertionArtifactsAsync(buildToInsert, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // *********** Look up existing PR ********************
                var gitClient = ProjectCollection.GetClient<GitHttpClient>();
                var branches = await gitClient.GetRefsAsync(
                    VSRepoId,
                    filter: $"heads/{Options.VisualStudioBranchName}",
                    cancellationToken: cancellationToken);
                var baseBranch = branches.Single(b => b.Name == $"refs/heads/{Options.VisualStudioBranchName}");

                var pullRequestId = Options.UpdateExistingPr;
                var useExistingPr = pullRequestId != 0;

                GitPullRequest pullRequest;
                string insertionBranchName;
                if (useExistingPr)
                {
                    pullRequest = await gitClient.GetPullRequestByIdAsync(pullRequestId, cancellationToken: cancellationToken);
                    insertionBranchName = pullRequest.SourceRefName.Substring("refs/heads/".Length);

                    var refs = await gitClient.GetRefsAsync(VSRepoId, filter: $"heads/{insertionBranchName}", cancellationToken: cancellationToken);
                    var insertionBranch = refs.Single(r => r.Name == $"refs/heads/{insertionBranchName}");

                    if (Options.OverwritePr)
                    {
                        // overwrite existing PR branch back to base before pushing new commit
                        var updateToBase = new GitRefUpdate
                        {
                            OldObjectId = insertionBranch.ObjectId,
                            NewObjectId = baseBranch.ObjectId,
                            Name = $"refs/heads/{insertionBranchName}"
                        };
                        var results = await gitClient.UpdateRefsAsync(new[] { updateToBase }, VSRepoId, cancellationToken: cancellationToken);
                        foreach (var result in results)
                        {
                            if (!result.Success)
                            {
                                LogError("Failed to overwrite PR: " + result.CustomMessage);
                            }
                        }
                    }
                    else
                    {
                        // not overwriting PR, so the insertion branch is actually the base
                        baseBranch = insertionBranch;
                    }
                }
                else
                {
                    pullRequest = null;
                    insertionBranchName = GetNewBranchName();
                }

                var allChanges = new List<GitChange>();

                var coreXT = await CoreXT.Load(gitClient, baseBranch.ObjectId);
                if (Options.InsertCoreXTPackages)
                {
                    // ************** Update Nuget Packages For Branch************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Updating Nuget Packages");
                    bool success;
                    (success, newPackageFiles) = UpdatePackages(
                        buildVersion,
                        coreXT,
                        insertionArtifacts.GetPackagesDirectory(),
                        cancellationToken);
                    retainBuild |= success;

                    // *********** Copy OptimizationInputs.props file ***********************
                    foreach (var propsFile in insertionArtifacts.GetOptProfPropertyFiles())
                    {
                        var targetFilePath = "src/Tests/config/runsettings/Official/OptProf/External/" + Path.GetFileName(propsFile);

                        var version = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = baseBranch.ObjectId };
                        var stream = await gitClient.GetItemContentAsync(VSRepoId, targetFilePath, download: true, versionDescriptor: version);
                        var originalContent = new StreamReader(stream).ReadToEnd();

                        var newContent = File.ReadAllText(propsFile);

                        if (GetChangeOpt(targetFilePath, originalContent, newContent) is GitChange change)
                        {
                            allChanges.Add(change);
                        }
                    }
                }

                if (Options.UpdateCoreXTLibraries || Options.UpdateAssemblyVersions)
                {
                    // ************** Update assembly versions ************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Updating assembly versions");
                    if (await UpdateAssemblyVersionsOpt(gitClient, baseBranch.ObjectId, insertionArtifacts) is GitChange assemblyVersionChange)
                    {
                        allChanges.Add(assemblyVersionChange);
                    }

                    // if we got this far then we definitely need to retain this build
                    retainBuild = true;
                }

                // *********** Update toolset ********************
                if (Options.InsertToolset)
                {
                    UpdateToolsetPackage(coreXT, insertionArtifacts, buildVersion);
                    retainBuild = true;
                }

                // ************ Update .corext\Configs\default.config ********************
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"Updating CoreXT default.config file");
                if (coreXT.SaveConfigOpt() is GitChange configChange)
                {
                    allChanges.Add(configChange);
                }

                // *********** Update .corext\Configs\components.json ********************

                BuildVersion oldComponentVersion = default;
                if (Options.InsertWillowPackages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Updating CoreXT components file");

                    var components = await GetLatestComponentsAsync(buildToInsert, cancellationToken);
                    var shouldSave = false;
                    foreach (var newComponent in components)
                    {
                        if (coreXT.TryGetComponentByName(newComponent.Name, out var oldComponent))
                        {
                            if (oldComponent.BuildVersion != default)
                            {
                                oldComponentVersion = oldComponent.BuildVersion;
                            }
                            coreXT.UpdateComponent(newComponent);
                            shouldSave = true;
                        }
                    }
                    if (shouldSave)
                    {
                        var allComponentChanges = coreXT.SaveComponents();
                        allChanges.AddRange(allComponentChanges);
                        retainBuild = true;
                    }
                }

                // ************* Ensure the build is retained on the servers *************
                if (Options.RetainInsertedBuild && retainBuild && !buildToInsert.KeepForever.GetValueOrDefault())
                {
                    Console.WriteLine("Marking inserted build for retention.");
                    buildToInsert.KeepForever = true;
                    var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
                    await buildClient.UpdateBuildAsync(buildToInsert);
                }

                // ************* Bail out if there are no changes ************************
                if (!allChanges.Any() && options.CherryPick.IsDefaultOrEmpty)
                {
                    LogWarning("No meaningful changes since the last insertion was merged. PR will not be created or updated.");
                    return (true, 0);
                }

                // ********************* Create push *************************************
                var currentCommit = baseBranch.ObjectId;
                if (allChanges.Any())
                {
                    var insertionBranchUpdate = new GitRefUpdate
                    {
                        Name = $"refs/heads/{insertionBranchName}",
                        OldObjectId = baseBranch.ObjectId
                    };

                    var commit = new GitCommitRef
                    {
                        Comment = $"Updating {Options.InsertionName} to {buildVersion}",
                        Changes = allChanges
                    };
                    var push = new GitPush
                    {
                        RefUpdates = new[] { insertionBranchUpdate },
                        Commits = new[] { commit }
                    };
                    push = await gitClient.CreatePushAsync(push, VSRepoId, cancellationToken: cancellationToken);
                    currentCommit = push.Commits.Single().CommitId;
                }

                // ********************* Cherry-pick VS commits *****************************
                var cherryPickCommits = Options.CherryPick;
                if (!cherryPickCommits.IsDefaultOrEmpty)
                {
                    Console.WriteLine("Cherry-picking the following VS commits:");
                    foreach (var cherryPickCommit in cherryPickCommits)
                    {
                        var gc = await gitClient.GetCommitAsync(cherryPickCommit, VSRepoId, cancellationToken: cancellationToken);
                        Console.WriteLine("- " + gc.RemoteUrl);
                    }
                    var commitRefs = cherryPickCommits.Select(id => new GitCommitRef() { CommitId = id }).ToArray();

                    var cherryPickBranchName = $"{insertionBranchName}-cherry-pick-{DateTime.Now:yyyyMMddHHmmss}";
                    var cherryPickArgs = new GitAsyncRefOperationParameters()
                    {
                        Source = new GitAsyncRefOperationSource()
                        {
                            CommitList = commitRefs
                        },
                        OntoRefName = $"refs/heads/{insertionBranchName}",
                        GeneratedRefName = $"refs/heads/{cherryPickBranchName}"
                    };
                    var cherryPick = await gitClient.CreateCherryPickAsync(cherryPickArgs, Options.TFSProjectName, VSRepoId, cancellationToken: cancellationToken);
                    while (cherryPick.Status < GitAsyncOperationStatus.Completed)
                    {
                        Console.WriteLine($"Cherry-pick progress: {cherryPick.DetailedStatus?.Progress ?? 0:P}");
                        await Task.Delay(5000);
                        cherryPick = await gitClient.GetCherryPickAsync(options.TFSProjectName, cherryPick.CherryPickId, VSRepoId, cancellationToken: cancellationToken);
                    }
                    Console.WriteLine($"Cherry-pick status: {cherryPick.Status}");

                    if (cherryPick.Status == GitAsyncOperationStatus.Completed)
                    {
                        var cherryPickBranch = await gitClient.GetBranchAsync(VSRepoId, cherryPickBranchName, cancellationToken: cancellationToken);
                        var addCherryPickedCommits = new GitRefUpdate
                        {
                            OldObjectId = currentCommit,
                            NewObjectId = cherryPickBranch.Commit.CommitId,
                            Name = $"refs/heads/{insertionBranchName}"
                        };
                        var results = await gitClient.UpdateRefsAsync(new[] { addCherryPickedCommits }, VSRepoId, cancellationToken: cancellationToken);
                        foreach (var result in results)
                        {
                            if (!result.Success)
                            {
                                LogError("Failed to reset ref to cherry-pick branch: " + result.CustomMessage);
                            }
                        }
                    }
                    else
                    {
                        LogError("Cherry-picking failed: " + cherryPick.DetailedStatus.FailureMessage);
                    }
                }

                // ********************* Create pull request *****************************
                var oldBuild = await GetSpecificBuildAsync(oldComponentVersion, cancellationToken);
                var prDescriptionMarkdown = CreatePullRequestDescription(oldBuild, buildToInsert, useMarkdown: true);

                if (buildToInsert.Result == BuildResult.PartiallySucceeded)
                {
                    prDescriptionMarkdown += Environment.NewLine + ":warning: The build being inserted has partially succeeded.";
                }

                if (!useExistingPr || Options.OverwritePr)
                {
                    try
                    {
                        var nl = Environment.NewLine;
                        if (oldBuild is null)
                        {
                            prDescriptionMarkdown += $"{nl}---{nl}Unable to find details for previous build ({oldComponentVersion}){nl}";
                        }
                        else
                        {
                            var (changes, diffLink) = await GetChangesBetweenBuildsAsync(oldBuild, buildToInsert, cancellationToken);

                            var diffDescription = changes.Any()
                                ? $"[View Complete Diff of Changes]({diffLink})"
                                : "No source changes since previous insertion";

                            prDescriptionMarkdown += nl + "---" + nl + diffDescription + nl;
                            prDescriptionMarkdown = AppendChangesToDescription(prDescriptionMarkdown, oldBuild ?? buildToInsert, changes);
                        }
                    }
                    catch (Exception e)
                    {
                        LogWarning("Failed to create diff links.");
                        LogWarning(e.Message);
                    }
                }

                if (useExistingPr)
                {
                    try
                    {
                        if (Options.OverwritePr)
                        {
                            pullRequest = await OverwritePullRequestAsync(pullRequestId, prDescriptionMarkdown, buildVersion.ToString(), options.TitlePrefix, cancellationToken);
                        }
                        pullRequestId = pullRequest.PullRequestId;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Unable to update pull request for '{pullRequest.SourceRefName}'");
                        LogError(ex);
                        return (false, 0);
                    }
                }
                else
                {
                    // create a new PR
                    Console.WriteLine($"Create Pull Request");
                    try
                    {
                        // If this insertion was queued for PR validation, for a dev branch, or for a feature branch,
                        // then add the build queuer as a reviewer instead of mlinfraswat.
                        var isPrValidation = !string.IsNullOrEmpty(GetBuildPRNumber(buildToInsert));
                        var isDevOrFeatureBranch = Options.BranchName.StartsWith("dev/") || Options.BranchName.StartsWith("features/");

                        var reviewerId = isPrValidation || isDevOrFeatureBranch
                            ? buildToInsert.RequestedBy.Id
                            : MLInfraSwatUserId.ToString();

                        pullRequest = await CreatePullRequestAsync(insertionBranchName, prDescriptionMarkdown, buildVersion.ToString(), options.TitlePrefix, reviewerId, cancellationToken);
                        if (pullRequest == null)
                        {
                            LogError($"Unable to create pull request for '{insertionBranchName}'");
                            return (false, 0);
                        }

                        pullRequestId = pullRequest.PullRequestId;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Unable to create pull request for '{insertionBranchName}'");
                        LogError(ex);
                        return (false, 0);
                    }
                }

                // ********************* Create validation build *****************************
                if (Options.QueueValidationBuild)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Create Validation Build");
                    try
                    {
                        if (Options.CreateDraftPr)
                        {
                            // When creating Draft PRs no policies are automatically started.
                            // If we do not queue a CloudBuild the Perf DDRITs request will
                            // spin waiting for a build to test against until it timesout.
                            await QueueBuildPolicy(pullRequest, "CloudBuild - PR");
                        }

                        await QueueBuildPolicy(pullRequest, "Request Perf DDRITs");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Unable to create a CloudBuild validation build for '{insertionBranchName}'");
                        LogWarning(ex);
                    }

                    if (Options.CreateDraftPr)
                    {
                        // When creating Draft PRs no policies are automatically started.
                        await TryQueueBuildPolicy(pullRequest, "Insertion Hash Check", insertionBranchName);
                        await TryQueueBuildPolicy(pullRequest, "Insertion Sign Check", insertionBranchName);
                        await TryQueueBuildPolicy(pullRequest, "Insertion Symbol Check", insertionBranchName);
                    }
                }

                // ********************* Set PR to Auto-Complete *****************************
                if (Options.SetAutoComplete)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Set PR to Auto-Complete");
                    try
                    {
                        var prDescriptionText = CreatePullRequestDescription(oldBuild, buildToInsert, useMarkdown: false);
                        await SetAutoCompleteAsync(pullRequest, prDescriptionText, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Unable to Set PR to Auto-Complete for '{insertionBranchName}'");
                        LogWarning(ex);
                    }
                }

                return (true, pullRequestId);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return (false, 0);
            }
            finally
            {
                Options = null;
            }
        }

        private static void LogWarning(object message)
        {
            Console.WriteLine("##vso[task.logissue type=warning] " + message);
        }

        private static void LogError(object message)
        {
            Console.WriteLine("##vso[task.logissue type=error] " + message);
        }

        private static void UpdateToolsetPackage(
            CoreXT coreXT,
            InsertionArtifacts artifacts,
            BuildVersion buildVersion)
        {
            Console.WriteLine("Updating toolset compiler package");

            var packagesDir = artifacts.GetPackagesDirectory();
            var toolsetPackagePath = Directory.EnumerateFiles(packagesDir,
                $"{PackageInfo.RoslynToolsetPackageName}*.nupkg",
                SearchOption.AllDirectories).Single();

            var fileName = Path.GetFileName(toolsetPackagePath);
            var package = PackageInfo.ParsePackageFileName(fileName);

            if (!coreXT.TryGetPackageVersion(package, out var previousPackageVersion))
            {
                throw new Exception("Toolset package is not installed in this enlistment");
            }

            UpdatePackage(previousPackageVersion, buildVersion, coreXT, package);
        }

        public static string ToFullString(this XDocument document)
        {
            return document.Declaration.ToString() + "\n" + document.ToString();
        }

        public static bool IsWhiteSpaceOnlyChange(string s1, string s2)
        {
            return removeNewlines(s1) == removeNewlines(s2);

            string removeNewlines(string s) => s.Replace("\r\n", "").Replace("\n", "");
        }

        public static GitChange GetChangeOpt(string path, string originalText, string newText)
        {
            if (!IsWhiteSpaceOnlyChange(originalText, newText))
            {
                return new GitChange
                {
                    ChangeType = VersionControlChangeType.Edit,
                    Item = new GitItem { Path = path },
                    // VS uses `* text=auto` which means that all files are normalized to LF line endings on checkin.
                    NewContent = new ItemContent() { Content = newText.Replace("\r\n", "\n"), ContentType = ItemContentType.RawText }
                };
            }
            else
            {
                return null;
            }
        }

        private static string CreatePullRequestDescription(Build oldBuild, Build buildToinsert, bool useMarkdown)
        {
            var oldBuildDescription = "";
            if (oldBuild is object)
            {
                oldBuildDescription = useMarkdown
                    ? $"from {oldBuild.GetBuildDescriptionMarkdown()} "
                    : $"from {oldBuild.GetBuildDescriptionText()} ";
            }

            var newBuildDescription = useMarkdown
                    ? $"to {buildToinsert.GetBuildDescriptionMarkdown()}"
                    : $"to {buildToinsert.GetBuildDescriptionText()}";

            var prValidationMessage = GetGitHubPullRequestUrlMessage(buildToinsert, useMarkdown);

            var documentationMessage = @"[Troubleshooting](onenote:https://microsoft.sharepoint.com/teams/managedlanguages/files/Team%20Notebook/Roslyn%20Team%20Notebook%20-%20New/Infrastructure.one#Life%20of%20a%20Tiger&section-id=%7B0E3C75FE-0827-41BF-9696-F5CD7105BC9A%7D&page-id=%7B96423CC8-6A3A-4ABF-B1BD-E4488D1EF099%7D&object-id=%7B7EBA739D-44DB-0180-278C-94C87152BFA5%7D&70)"
                + " [(don't use the web view)](https://microsoft.sharepoint.com/teams/managedlanguages/_layouts/OneNote.aspx?id=%2Fteams%2Fmanagedlanguages%2Ffiles%2FTeam%20Notebook%2FRoslyn%20Team%20Notebook%20-%20New&wd=target%28Infrastructure.one%7C0E3C75FE-0827-41BF-9696-F5CD7105BC9A%2FLife%20of%20a%20Tiger%7C96423CC8-6A3A-4ABF-B1BD-E4488D1EF099%2F%29)"
                + Environment.NewLine;

            return $"Updating {Options.InsertionName} {oldBuildDescription}{newBuildDescription}{Environment.NewLine}{prValidationMessage}{documentationMessage}";
        }

        private static string GetGitHubPullRequestUrlMessage(Build build, bool useMarkdown)
        {
            var prValidationMessage = string.Empty;

            if (build.Repository.Type == "GitHub")
            {
                var repoURL = $"http://github.com/{build.Repository.Id}";

                string prNumber = GetBuildPRNumber(build);
                if (!string.IsNullOrEmpty(prNumber))
                {
                    var prUrl = GetGitHubPullRequestUrl(repoURL, prNumber);
                    prValidationMessage = useMarkdown
                        ? $"This is a PR validation build for [{prNumber}]({prUrl})"
                        : $"This is a PR validation build for {prUrl}";
                }
            }

            return prValidationMessage;
        }

        private static string GetBuildPRNumber(Build build)
        {
            return build.Tags.FirstOrDefault(t => t.StartsWith(PRBuildTagPrefix))?.Substring(PRBuildTagPrefix.Length);
        }

        public static string GetBuildDescriptionMarkdown(this Build build)
        {
            var number = build.BuildNumber;
            var shortCommitId = build.SourceVersion.Substring(0, 7);

            var url = getLink(build, "web");
            var commitUrl = getLink(build, "sourceVersionDisplayUri");

            return $"[{number}]({url}) ([{shortCommitId}]({commitUrl}))";

            static string getLink(Build build, string key)
            {
                if (build.Links.Links.TryGetValue(key, out var obj))
                {
                    var sourceLink = (ReferenceLink)obj;
                    return sourceLink.Href;
                }
                return string.Empty;
            }
        }

        public static string GetBuildDescriptionText(this Build build)
        {
            var number = build.BuildNumber;
            var shortCommitId = build.SourceVersion.Substring(0, 7);

            return $"{number} ({shortCommitId})";
        }
    }
}
