// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibGit2Sharp;

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
                    Console.WriteLine($"Could not authenticate with {Options.VSTSUri}");
                    Console.WriteLine(ex);
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
                        Console.WriteLine($"Unable to create placeholder PR for '{options.VisualStudioBranchName}'");
                        Console.WriteLine(ex);
                        return (false, 0);
                    }

                    if (dummyPR == null)
                    {
                        Console.WriteLine($"Unable to create placeholder PR for '{options.VisualStudioBranchName}'");
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

                var enlistmentRoot = GetAbsolutePathForEnlistment();

                var allChanges = new List<GitChange>();

                var coreXT = CoreXT.Load(ProjectCollection.GetClient<GitHttpClient>(), Options);
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
                        var content = File.ReadAllText(propsFile);
                        var change = new GitChange
                        {
                            ChangeType = VersionControlChangeType.Edit,
                            Item = new GitItem { Path = targetFilePath },
                            NewContent = new ItemContent()
                            {
                                Content = content,
                                ContentType = ItemContentType.RawText
                            }
                        };
                        allChanges.Add(change);
                    }
                }

                if (Options.UpdateCoreXTLibraries || Options.UpdateAssemblyVersions)
                {
                    // ************** Update assembly versions ************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Updating assembly versions");
                    var assemblyVersionChanges = UpdateAssemblyVersions(insertionArtifacts);
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
                var configChange = coreXT.SaveConfig();
                allChanges.Add(configChange);

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

                // ********************* Verify Build Completes **************************
                if (Options.PartitionsToBuild != null)
                {
                    // TODO: remove PartitionsToBuild command line option
                }

                // ********************* Create push *************************************
                var gitClient = ProjectCollection.GetClient<GitHttpClient>();
                var branches = await gitClient.GetRefsAsync(
                    VSRepoId,
                    filter: $"heads/{Options.VisualStudioBranchName}",
                    cancellationToken: cancellationToken);
                var baseBranch = branches.Single(b => b.Name == $"refs/heads/{Options.VisualStudioBranchName}");

                var newBranchName = string.IsNullOrEmpty(Options.NewBranchName) ? Options.NewBranchName : GetNewBranchName();
                var newBranch = new GitRefUpdate
                {
                    Name = $"refs/heads/{newBranchName}",
                    OldObjectId = baseBranch.ObjectId
                };

                var commit = new GitCommitRef
                {
                    Comment = $"Updating {Options.InsertionName} to {buildVersion}",
                    Changes = allChanges
                };
                var push = new GitPush
                {
                    RefUpdates = new[] { newBranch },
                    Commits = new[] { commit }
                };

                // TODO: do we need to specify --force? how can we do that?
                push = await gitClient.CreatePushAsync(push, VSRepoId, cancellationToken: cancellationToken);

                // ********************* Create pull request *****************************
                var pullRequestId = Options.UpdateExistingPr;
                var useExistingPr = pullRequestId != 0;

                var prDescription = $"Updating {Options.InsertionName} to {buildVersion} ([{commitSHA}]({lastCommitUrl}))";
                try
                {
                    // TODO: maybe skip if useExistingPR && !Options.OverwritePR
                    var oldBuild = await GetSpecificBuildAsync(oldComponentVersion, cancellationToken);
                    var (changes, diffLink) = await GetChangesBetweenBuildsAsync(oldBuild ?? buildToInsert, buildToInsert, cancellationToken);
                    prDescription = AppendDiffToDescription(prDescription, diffLink);
                    prDescription = AppendChangesToDescription(prDescription, oldBuild ?? buildToInsert, changes);
                }
                catch (Exception e)
                {
                    Console.WriteLine("##vso[task.logissue type=warning] Failed to create diff links.");
                    Console.WriteLine($"##vso[task.logissue type=warning] {e.Message}");
                }

                GitPullRequest pullRequest;
                if (useExistingPr)
                {
                    pullRequest = await gitClient.GetPullRequestByIdAsync(pullRequestId, cancellationToken: cancellationToken);
                    try
                    {
                        if (Options.OverwritePr)
                        {
                            pullRequest = await UpdatePullRequestDescriptionAsync(pullRequestId, prDescription, cancellationToken);
                        }
                        pullRequestId = pullRequest.PullRequestId;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to update pull request for '{pullRequest.SourceRefName}'");
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
                        pullRequest = await CreatePullRequestAsync(newBranchName, prDescription, buildVersion.ToString(), options.TitlePrefix, cancellationToken);
                        if (pullRequest == null)
                        {
                            Console.WriteLine($"Unable to create pull request for '{newBranchName}'");
                            return (false, 0);
                        }

                        pullRequestId = pullRequest.PullRequestId;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to create pull request for '{newBranchName}'");
                        Console.WriteLine(ex);
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
                        Console.WriteLine($"Unable to create a CloudBuild validation build for '{newBranchName}'");
                        Console.WriteLine(ex);
                    }
                }

                return (true, pullRequestId);
            }
            catch (RepositoryNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(@"Please ensure a VS enlistment exists at the given path, or pass the `/enlistmentpath=C:\path\to\VS` argument on the command line.");
                return (false, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return (false, 0);
            }
            finally
            {
                Options = null;
            }
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
            return document.Declaration.ToString() + Environment.NewLine + document.ToString();
        }

        // TODO: either delete or actually use this
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
