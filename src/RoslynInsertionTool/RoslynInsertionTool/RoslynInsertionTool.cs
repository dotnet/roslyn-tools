// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Roslyn.Insertion
{
    public static partial class RoslynInsertionTool
    {
        private static List<string> WarningMessages { get; } = new List<string>();

        private static RoslynInsertionToolOptions Options { get; set; }

        /// <returns>A tuple containing (success, pullRequestId).</returns>
        public static async Task<(bool, int)> PerformInsertionAsync(
            RoslynInsertionToolOptions options,
            CancellationToken cancellationToken)
        {
            Options = options;
            Console.WriteLine($"{Environment.NewLine}New Insertion Into {Options.VisualStudioBranchName} Started{Environment.NewLine}");

            GitPullRequest pullRequest = null;
            var shouldRollBackGitChanges = false;
            var newPackageFiles = new List<string>();

            try
            {
                // Verify that the arguments we were passed authenticate correctly
                Console.WriteLine($"Verifying given authentication for {Options.VSTSUri}");
                try
                {
                    ProjectCollection.Authenticate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not authenticate with {Options.VSTSUri}");
                    Console.WriteLine(ex);
                    return (false, 0);
                }

                Console.WriteLine($"Verification succeeded for {Options.VSTSUri}");

                // ********************** Create dummy PR *****************************
                if (Options.CreateDummyPr)
                {
                    try
                    {
                        pullRequest = await CreatePlaceholderBranchAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to create placeholder PR for '{options.VisualStudioBranchName}'");
                        Console.WriteLine(ex);
                        return (false, 0);
                    }

                    if (pullRequest == null)
                    {
                        Console.WriteLine($"Unable to create placeholder PR for '{options.VisualStudioBranchName}'");
                        return (false, 0);
                    }

                    return (true, pullRequest.PullRequestId);
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
                Branch branch = null;
                cancellationToken.ThrowIfCancellationRequested();
                var useExistingPr = Options.UpdateExistingPr != 0;
                if (useExistingPr)
                {
                    // ****************** Update existing PR ***********************
                    pullRequest = await GetExistingPullRequestAsync(Options.UpdateExistingPr, cancellationToken);
                    branch = SwitchToBranchAndUpdate(pullRequest.SourceRefName, Options.VisualStudioBranchName, overwriteExistingChanges: Options.OverwritePr);
                }
                else
                {
                    // ****************** Create Branch ***********************
                    Console.WriteLine("Creating New Branch");
                    branch = string.IsNullOrEmpty(Options.NewBranchName)
                        ? null
                        : CreateBranch(cancellationToken);
                }

                shouldRollBackGitChanges = branch != null;

                var enlistmentRoot = GetAbsolutePathForEnlistment();
                var coreXT = CoreXT.Load(enlistmentRoot);

                if (Options.InsertCoreXTPackages)
                {
                    // ************** Update Nuget Packages For Branch************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Updating Nuget Packages");
                    bool success = false;
                    (success, newPackageFiles) = UpdatePackages(
                        buildVersion,
                        coreXT,
                        insertionArtifacts.GetPackagesDirectory(),
                        cancellationToken);
                    retainBuild |= success;

                    // ************ Update .corext\Configs\default.config ********************
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Updating CoreXT default.config file");
                    coreXT.SaveConfig();

                    // *********** Copy OptimizationInputs.props file ***********************
                    foreach (var propsFile in insertionArtifacts.GetOptProfPropertyFiles())
                    {
                        var targetFilePath = Path.Combine(enlistmentRoot, @"src\Tests\config\runsettings\Official\OptProf", Path.GetFileName(propsFile));
                        Console.WriteLine($"Updating {targetFilePath}");
                        File.Copy(propsFile, targetFilePath, overwrite: true);
                    }
                }

                if (Options.UpdateCoreXTLibraries || Options.UpdateAssemblyVersions)
                {
                    // ************** Update assembly versions ************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Updating assembly versions");
                    UpdateAssemblyVersions(insertionArtifacts);

                    // if we got this far then we definitely need to retain this build
                    retainBuild = true;
                }

                // *********** Update toolset ********************
                if (Options.InsertToolset)
                {
                    UpdateToolsetPackage(insertionArtifacts, buildVersion, cancellationToken);
                    retainBuild = true;
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
                        coreXT.SaveComponents();
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

                // ********************* Verify Build Completes **************************
                if (Options.PartitionsToBuild != null)
                {
                    Console.WriteLine($"Verifying build succeeds with changes");
                    foreach (var partition in Options.PartitionsToBuild)
                    {
                        Console.WriteLine($"Starting build of {partition}");

                        if (!(await CanBuildPartitionAsync(partition, cancellationToken)))
                        {
                            Console.WriteLine($"Build of partition {partition} failed");
                            return (false, 0);
                        }

                        Console.WriteLine($"Build of partition {partition} succeeded");
                    }
                }

                // ********************* Trigger a release *****************************
                Console.WriteLine($"Triggering a release for the build {buildToInsert.BuildNumber}");

                var release = await CreateReleaseAsync(buildToInsert, cancellationToken);

                // The timeout for the below wait is primarily dependent on:
                // 1. The release task itself - Since its currently only triggering symbol archival,
                //    it should not be very long but this should increase when more time intesive tasks are added to the release.
                // 2. The availability of machines to run the release on. This could be a problem at peak pool load
                //    where getting a machine can take upto an hour or more.
                WaitForReleaseCompletion(release, TimeSpan.FromMinutes(10), cancellationToken);

                Console.WriteLine($"Release succesfully triggered");

                // ********************* Create pull request *****************************
                var pullRequestId = 0;
                if (branch != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var prDescription = $"Updating {Options.InsertionName} to {buildVersion} ([{commitSHA}]({lastCommitUrl}))";
                    if (useExistingPr && pullRequest != null)
                    {
                        // update an existing pr
                        try
                        {
                            branch = PushChanges(branch, buildVersion, cancellationToken, forcePush: true);
                            if (Options.OverwritePr)
                            {
                                pullRequest = await UpdatePullRequestDescriptionAsync(Options.UpdateExistingPr, prDescription, cancellationToken);
                            }
                            shouldRollBackGitChanges = false;
                            pullRequestId = pullRequest.PullRequestId;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unable to update pull request for '{branch.FriendlyName}'");
                            Console.WriteLine(ex);
                            return (false, 0);
                        }
                    }
                    else
                    {
                        // create a new PR
                        Console.WriteLine($"Create Pull Request");
                        try
                        {
                            var oldBuild = await GetSpecificBuildAsync(oldComponentVersion, cancellationToken);
                            var (changes, diffLink) = await GetChangesBetweenBuildsAsync(oldBuild ?? buildToInsert, buildToInsert, cancellationToken);
                            prDescription = AppendDiffToDescription(prDescription, diffLink);
                            prDescription = AppendChangesToDescription(prDescription, changes);
                            branch = PushChanges(branch, buildVersion, cancellationToken);
                            pullRequest = await CreatePullRequestAsync(branch.FriendlyName, prDescription, buildVersion.ToString(), options.TitlePrefix, cancellationToken);
                            shouldRollBackGitChanges = false;
                            pullRequestId = pullRequest.PullRequestId;
                        }
                        catch (EmptyCommitException ecx)
                        {
                            Console.WriteLine($"Unable to create pull request for '{branch.FriendlyName}'");
                            Console.WriteLine(ecx);
                            return (false, 0);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unable to create pull request for '{branch.FriendlyName}'");
                            Console.WriteLine(ex);
                            return (false, 0);
                        }
                    }

                    if (pullRequest == null)
                    {
                        Console.WriteLine($"Unable to create pull request for '{branch.FriendlyName}'");
                        return (false, 0);
                    }
                }

                // ********************* Create validation build *****************************
                if (Options.QueueValidationBuild)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Create Validation Build");

                    if (pullRequest == null)
                    {
                        Console.WriteLine("Unable to create a validation build: no pull request.");
                        return (false, 0);
                    }

                    try
                    {
                        await QueueBuildPolicy(pullRequest, "CloudBuild - Request RPS");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to create a CloudBuild validation build for '{pullRequest.SourceRefName}'");
                        Console.WriteLine(ex);
                    }
                }

                return (true, pullRequestId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return (false, 0);
            }
            finally
            {
                // ********************* Rollback Git Changes ****************************
                if (shouldRollBackGitChanges)
                {
                    try
                    {
                        Console.WriteLine("Rolling back git changes");
                        var rollBackCommit = Enlistment.Branches[Options.VisualStudioBranchName].Commits.First();
                        Enlistment.Reset(ResetMode.Hard, rollBackCommit);
                        Enlistment.RemoveUntrackedFiles();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                Options = null;
            }
        }

        private static void UpdateToolsetPackage(
            InsertionArtifacts artifacts,
            BuildVersion buildVersion,
            CancellationToken cancellationToken)
        {
            Console.WriteLine("Updating toolset compiler package");

            var packagesDir = artifacts.GetPackagesDirectory();
            var toolsetPackagePath = Directory.EnumerateFiles(packagesDir,
                $"{PackageInfo.RoslynToolsetPackageName}*.nupkg",
                SearchOption.AllDirectories).Single();

            var fileName = Path.GetFileName(toolsetPackagePath);
            var package = PackageInfo.ParsePackageFileName(fileName);

            var coreXT = CoreXT.Load(GetAbsolutePathForEnlistment());
            if (!coreXT.TryGetPackageVersion(package, out var previousPackageVersion))
            {
                throw new Exception("Toolset package is not installed in this enlistment");
            }

            UpdatePackage(previousPackageVersion, buildVersion, coreXT, package);

            // Update .corext/Configs/default.config
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine("Updating CoreXT config file");
            coreXT.SaveConfig();
        }

        private static string GetHTMLSuccessMessage(GitPullRequest pullRequest, List<string> newPackageFiles)
        {
            const string greenSpan = "<span style =\"color: green\">";
            const string redSpan = "<span style =\"color: red\">";
            const string orangeSpan = "<span style =\"color: OrangeRed\">";
            const string endSpan = "</span>";

            var bodyHtml = new StringBuilder();
            bodyHtml.AppendLine();
            bodyHtml.AppendLine($"{greenSpan} Insertion Succeeded {endSpan}");
            bodyHtml.AppendLine();
            bodyHtml.AppendLine($"Review pull request <a href=\"{Options.VSTSUri}/{Options.TFSProjectName}/_git/VS/pullrequest/{pullRequest.PullRequestId}\">here</a>");
            bodyHtml.AppendLine();

            if (newPackageFiles.Count > 0)
            {
                foreach (var packageFileName in newPackageFiles)
                {
                    bodyHtml.AppendLine($"{redSpan} New package(s) inserted {packageFileName}{endSpan}");
                }

                bodyHtml.AppendLine($@"Make sure the following files as well as all appid\**\*.config.tt files are updated appropriately:");
                foreach (var path in VersionsUpdater.RelativeFilePaths)
                {
                    bodyHtml.AppendLine(path);
                }
            }

            if (WarningMessages.Count > 0)
            {
                bodyHtml.AppendLine("NOTE there were unexpected warnings during this insertion:");
                foreach (var message in WarningMessages)
                {
                    bodyHtml.AppendLine($"{orangeSpan}{message}{endSpan}");
                }
            }

            return bodyHtml.ToString().Replace("\n", "<br/>");
        }
    }
}
