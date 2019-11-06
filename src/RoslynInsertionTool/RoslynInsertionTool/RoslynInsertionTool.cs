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
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Roslyn.Insertion
{
    public static partial class RoslynInsertionTool
    {
        public static readonly Guid VSRepoId = new Guid("a290117c-5a8a-40f7-bc2c-f14dbe3acf6d");

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

                string commitSHA = buildToInsert.SourceVersion.Substring(0, 7);
                string lastCommitUrl = string.Empty;
                if (buildToInsert.Links.Links.ContainsKey("sourceVersionDisplayUri"))
                {
                    // Get a link to the commit the build was built from.
                    var sourceLink = (ReferenceLink)buildToInsert.Links.Links["sourceVersionDisplayUri"];
                    lastCommitUrl = sourceLink.Href;
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
                        await gitClient.UpdateRefsAsync(new[] { updateToBase }, VSRepoId, cancellationToken: cancellationToken);
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

                var coreXT = CoreXT.Load(gitClient, baseBranch.ObjectId);
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
                    var assemblyVersionChanges = UpdateAssemblyVersions(gitClient, baseBranch.ObjectId, insertionArtifacts);
                    allChanges.AddRange(assemblyVersionChanges);

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
                    await buildClient.UpdateBuildAsync(buildToInsert, buildToInsert.Id);
                }

                // ************* Bail out if there are no changes ************************
                if (!allChanges.Any())
                {
                    LogWarning("No meaningful changes since the last insertion was merged. PR will not be created or updated.");
                    return (true, 0);
                }

                // ********************* Create push *************************************
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

                await gitClient.CreatePushAsync(push, VSRepoId, cancellationToken: cancellationToken);

                // ********************* Create pull request *****************************

                var prDescription = $"Updating {Options.InsertionName} to {buildVersion} ([{commitSHA}]({lastCommitUrl}))";
                if (!useExistingPr || Options.OverwritePr)
                {
                    try
                    {
                        var nl = Environment.NewLine;
                        var oldBuild = await GetSpecificBuildAsync(oldComponentVersion, cancellationToken);
                        if (oldBuild is null)
                        {
                            prDescription += $"{nl}---{nl}Unable to find details for previous build ({oldComponentVersion}).{nl}";
                        }
                        else
                        {
                            var (changes, diffLink) = await GetChangesBetweenBuildsAsync(oldBuild, buildToInsert, cancellationToken);

                            var diffDescription = changes.Any()
                                ? $"[View Complete Diff of Changes]({diffLink})"
                                : "No source changes since previous insertion";

                            prDescription += nl + "---" + nl + diffDescription + nl;
                            prDescription = AppendChangesToDescription(prDescription, oldBuild ?? buildToInsert, changes);
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
                            pullRequest = await gitClient.UpdatePullRequestAsync(
                                new GitPullRequest { Description = prDescription },
                                VSRepoId,
                                pullRequestId,
                                cancellationToken: cancellationToken);
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
                        pullRequest = await CreatePullRequestAsync(insertionBranchName, prDescription, buildVersion.ToString(), options.TitlePrefix, cancellationToken);
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
                        await QueueBuildPolicy(pullRequest, "Request Perf DDRITs");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Unable to create a CloudBuild validation build for '{insertionBranchName}'");
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
    }
}
