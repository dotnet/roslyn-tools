// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;

using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

using NLog;

namespace Roslyn.Insertion
{
    public static partial class RoslynInsertionTool
    {
        private const string LogFilePath = "rit.log";

        private static ILogger Log { get; set; }

        private static List<string> WarningMessages { get; } = new List<string>();

        private static RoslynInsertionToolOptions Options { get; set; }

        public static async Task PerformInsertionAsync(
            RoslynInsertionToolOptions options,
            ILogger log,
            CancellationToken cancellationToken)
        {
            Options = options;
            Log = log;
            File.Delete(LogFilePath);
            Log.Info($"{Environment.NewLine}New Insertion Into {Options.VisualStudioBranchName} Started{Environment.NewLine}");

            GitPullRequest pullRequest = null;
            var shouldRollBackGitChanges = false;
            var newPackageFiles = new List<string>();
            var isInsertionCancelled = false;

            try
            {
                // Verify that the arguments we were passed authenticate correctly
                Log.Trace($"Verifying given authentication for {Options.VSTSUri}");
                try
                {
                    ProjectCollection.Authenticate();
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not authenticate with {Options.VSTSUri}");
                    Log.Error(ex);
                    return;
                }

                Log.Trace($"Verification succeeded for {Options.VSTSUri}");

                // ********************** Get Last Insertion *****************************
                cancellationToken.ThrowIfCancellationRequested();

                BuildVersion buildVersion;
                Build newestBuild;
                bool retainBuild = false;
                // Get the version from TFS build queue, e.g. Roslyn-Master-Signed-Release.
                // We assume all CoreXT packages we build (Roslyn and all dependencies we
                // insert) have the same version.
                if (string.IsNullOrEmpty(Options.SpecificBuild))
                {
                    newestBuild = await GetLatestBuildAsync(cancellationToken);
                    buildVersion = BuildVersion.FromTfsBuildNumber(newestBuild.BuildNumber, Options.BuildQueueName);
                }
                else
                {
                    buildVersion = BuildVersion.FromString(Options.SpecificBuild);
                    newestBuild = await GetSpecificBuildAsync(buildVersion, cancellationToken);
                }

                // ****************** Get Latest and Create Branch ***********************
                cancellationToken.ThrowIfCancellationRequested();
                Log.Info($"Getting Latest From {Options.VisualStudioBranchName} and Creating New Branch");
                var branch = string.IsNullOrEmpty(Options.NewBranchName)
                    ? null
                    : GetLatestAndCreateBranch(cancellationToken);
                shouldRollBackGitChanges = branch != null;

                if (Options.UpdateCoreXTLibraries)
                {
                    // ************** Update paths to CoreFX libraries ************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Update paths to CoreFX libraries");
                    if (!await TryUpdateFileAsync(
                        Path.Combine("ProductData", "ContractAssemblies.props"),
                        buildVersion,
                        onlyCopyIfFileDoesNotExistAtDestination: false,
                        cancellationToken: cancellationToken))
                    {
                        return;
                    }
                }

                var coreXT = CoreXT.Load(GetAbsolutePathForEnlistment());

                if (Options.InsertCoreXTPackages)
                {
                   // ************** Update Nuget Packages For Branch************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Updating Nuget Packages");
                    bool success = false;
                    (success, newPackageFiles) = UpdatePackages(
                        buildVersion,
                        coreXT,
                        GetPackagesDirPath(buildVersion),
                        cancellationToken);
                    retainBuild |= success;

                    // ************ Update .corext\Configs\default.config ********************
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Updating CoreXT config file");
                    coreXT.SaveConfig();

                    // ************** Update paths to CoreFX libraries ************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Update paths to CoreFX libraries");
                    if (!await TryUpdateFileAsync(
                        Path.Combine("ProductData", "ContractAssemblies.props"),
                        buildVersion,
                        onlyCopyIfFileDoesNotExistAtDestination: false,
                        cancellationToken: cancellationToken))
                    {
                        return;
                    }

                    // ************** Update assembly versions ************************
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Updating assembly versions");
                    UpdateAssemblyVersions(buildVersion);

                    // if we got this far then we definitely need to retain this build
                    retainBuild = true;
                }

                // *********** Update toolset ********************
                if (Options.InsertToolset)
                {
                    UpdateToolsetPackage(buildVersion, cancellationToken);
                    retainBuild = true;
                }

                // *********** Update .corext\Configs\components.json ********************
                if (Options.InsertWillowPackages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Updating CoreXT components file");

                    var components = await GetLatestComponentsAsync(newestBuild, cancellationToken);
                    var shouldSave = false;
                    foreach (var newComponent in components)
                    {
                        if (coreXT.TryGetComponentByName(newComponent.Name, out var oldComponent))
                        {
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
                if (Options.RetainInsertedBuild && retainBuild && !newestBuild.KeepForever.GetValueOrDefault())
                {
                    Log.Info("Marking inserted build for retention.");
                    newestBuild.KeepForever = true;
                    var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
                    await buildClient.UpdateBuildAsync(newestBuild, newestBuild.Id);
                }

                // ********************* Verify Build Completes **************************
                if (Options.PartitionsToBuild != null)
                {
                    Log.Info($"Verifying build succeeds with changes");
                    foreach (var partition in Options.PartitionsToBuild)
                    {
                        Log.Info($"Starting build of {partition}");

                        if (!(await CanBuildPartitionAsync(partition, cancellationToken)))
                        {
                            Log.Error($"Build of partition {partition} failed");
                            return;
                        }

                        Log.Info($"Build of partition {partition} succeeded");
                    }
                }

                // ********************* Trigger a release *****************************
                Log.Info($"Triggering a release for the build {newestBuild.BuildNumber}");

                var release = await CreateReleaseAsync(newestBuild, cancellationToken);

                // The timeout for the below wait is primarily dependent on:
                // 1. The release task itself - Since its currently only triggering symbol archival,
                //    it should not be very long but this should increase when more time intesive tasks are added to the release.
                // 2. The availability of machines to run the release on. This could be a problem at peak pool load
                //    where getting a machine can take upto an hour or more.
                WaitForReleaseCompletion(release, TimeSpan.FromMinutes(10), cancellationToken);

                Log.Info($"Release succesfully triggered");

                // ********************* Create pull request *****************************
                if (branch != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Create Pull Request");
                    try
                    {
                        PushChanges(branch, buildVersion, cancellationToken);
                        pullRequest = await CreatePullRequestAsync(branch.FriendlyName, $"Updating {Options.InsertionName} to {buildVersion}", cancellationToken);
                        shouldRollBackGitChanges = false;
                    }
                    catch (EmptyCommitException ecx)
                    {
                        isInsertionCancelled = true;

                        Log.Warn($"Unable to create pull request for '{branch.FriendlyName}'");
                        Log.Warn(ecx);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Unable to create pull request for '{branch.FriendlyName}'");
                        Log.Error(ex);
                        return;
                    }

                    if (pullRequest == null)
                    {
                        Log.Error($"Unable to create pull request for '{branch.FriendlyName}'");
                        return;
                    }
                }

                // ********************* Create validation build *****************************
                if (Options.QueueValidationBuild)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.Info($"Create Validation Build");

                    if (pullRequest == null)
                    {
                        Log.Error("Unable to create a validation build: no pull request.");
                        return;
                    }

                    try
                    {
                        await QueueBuildPolicy(pullRequest, "VAL build with DDRITs and RPS");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Unable to create a validation build for '{pullRequest.SourceRefName}'");
                        Log.Error(ex);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OutdatedPackageException || ex is OperationCanceledException)
                {
                    isInsertionCancelled = true;
                }

                Log.Error(ex);
            }
            finally
            {
                // ************************* Flush Log ***********************************
                Log.Factory.Flush();

                // ********************* Rollback Git Changes ****************************
                if (shouldRollBackGitChanges)
                {
                    try
                    {
                        Log.Info("Rolling back git changes");
                        var rollBackCommit = Enlistment.Branches[Options.VisualStudioBranchName].Commits.First();
                        Enlistment.Reset(ResetMode.Hard, rollBackCommit);
                        Enlistment.RemoveUntrackedFiles();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }

                // ********************* Send Status Mail ********************************
                if (!string.IsNullOrEmpty(Options.EmailServerName) &&
                    !string.IsNullOrEmpty(Options.MailRecipient))
                {
                    try
                    {
                        SendMail(pullRequest, newPackageFiles, isInsertionCancelled);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Unable to send mail, EmailServerName: '{Options.EmailServerName}', MailRecipient: '{Options.MailRecipient}'");
                        Log.Error(ex);
                    }
                }

                Options = null;
                Log = null;
            }
        }

        private static void UpdateToolsetPackage(
            BuildVersion buildVersion,
            CancellationToken cancellationToken)
        {
            Log.Info("Updating toolset compiler package");

            var packagesDir = GetPackagesDirPath(buildVersion);
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
            Log.Info("Updateing CoreXT config file");
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

        private static void SendMail(GitPullRequest pullRequest, List<string> newPackageFiles, bool isInsertionCancelled = false)
        {
            Log.Factory.Flush();
            using (var mailClient = new SmtpClient(Options.EmailServerName))
            {
                mailClient.UseDefaultCredentials = true;

                var from = new MailAddress(Options.Username);
                var to = new MailAddress(Options.MailRecipient);
                using (var mailMessage = new MailMessage(from, to))
                {
                    if (pullRequest != null)
                    {
                        mailMessage.Subject = $"{Options.InsertionName} insertion from {Options.BuildQueueName}/{Options.BranchName}/{Options.BuildConfig} into {Options.VisualStudioBranchName} SUCCEEDED";
                        mailMessage.SubjectEncoding = Encoding.UTF8;
                        mailMessage.IsBodyHtml = true;
                        mailMessage.Body = GetHTMLSuccessMessage(pullRequest, newPackageFiles);
                        mailMessage.BodyEncoding = Encoding.UTF8;
                    }
                    else
                    {
                        var insertionStatus = isInsertionCancelled ? "CANCELLED" : "FAILED";

                        mailMessage.Subject = $"{Options.InsertionName} insertion from {Options.BuildQueueName}/{Options.BranchName}/{Options.BuildConfig} into {Options.VisualStudioBranchName} {insertionStatus}";
                        mailMessage.SubjectEncoding = Encoding.UTF8;
                        mailMessage.Body = $"Review attached log for details";
                    }

                    if (File.Exists(LogFilePath))
                    {
                        mailMessage.Attachments.Add(new System.Net.Mail.Attachment(LogFilePath));
                    }

                    mailClient.Send(mailMessage);
                }
            }
        }
    }
}
