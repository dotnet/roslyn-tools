// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using LibGit2Sharp;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static readonly Lazy<Repository> LazyEnlistment = new Lazy<Repository>(() =>
        {
            var absolutePath = GetAbsolutePathForEnlistment();
            Log.Trace($"Creating git repository object for {absolutePath}");
            return new Repository(absolutePath);
        });

        private static readonly Lazy<Signature> LazyInsertionToolSignature = new Lazy<Signature>(() => new Signature("Roslyn Insertion Tool", Options.Username, DateTimeOffset.Now));

        private static Repository Enlistment => LazyEnlistment.Value;

        private static Signature InsertionToolSignature => LazyInsertionToolSignature.Value;

        private static Branch CheckoutBranch(Repository enlistment, string branchName, Branch newlocalBranch, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stopwatch watch;
            Log.Info($"Checking out branch {branchName}");
            watch = Stopwatch.StartNew();
            newlocalBranch = enlistment.Checkout(newlocalBranch, GetCheckoutOptions());
            Log.Trace($"Checking out branch took {watch.Elapsed.TotalSeconds} seconds");

            if (newlocalBranch.IsCurrentRepositoryHead)
            {
                Log.Trace($"{branchName} is the current repository head.");
            }
            else
            {
                Log.Trace($"{branchName} is NOT the current repository head.");
            }

            return newlocalBranch;
        }

        private static void CommitStagedChanges(BuildVersion newRoslynVersion, CancellationToken cancellationToken)
        {
            var message = $"Updating {Options.InsertionName} to {newRoslynVersion}";

            Stopwatch watch;
            cancellationToken.ThrowIfCancellationRequested();
            Log.Info($"Committing file(s)");
            watch = Stopwatch.StartNew();
            var commit = Enlistment.Commit(
                message, InsertionToolSignature, InsertionToolSignature);
            Log.Trace($"Committing took {watch.Elapsed.TotalSeconds} seconds");
        }

        private static Branch CreateBranch(Repository enlistment, FetchOptions fetchOptions, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Fetch latest changes
            FetchLatest(enlistment, fetchOptions);

            // Create branch and remote tracking branch
            CreateNewBranch(enlistment, cancellationToken, out string branchName, out var newLocalBranch);

            // Checkout Branch
            newLocalBranch = CheckoutBranch(enlistment, branchName, newLocalBranch, cancellationToken);

            return newLocalBranch;
        }

        private static void CreateNewBranch(Repository enlistment, CancellationToken cancellationToken, out string branchName, out Branch newlocalBranch)
        {
            Stopwatch watch;
            cancellationToken.ThrowIfCancellationRequested();
            branchName = $"{Options.NewBranchName}{Options.VisualStudioBranchName.Split('/').Last()}.{DateTime.Now:yyyyMMddHHmmss}";
            Log.Info($"Creating new branch {branchName}");
            var remoteTrackingBranchName = "origin/" + Options.VisualStudioBranchName;
            var remoteTrackingBranch = enlistment.Branches[remoteTrackingBranchName];
            watch = Stopwatch.StartNew();
            newlocalBranch = enlistment.CreateBranch(branchName, remoteTrackingBranch.Tip);
            newlocalBranch = enlistment.Branches.Update(newlocalBranch, b => { b.Remote = "origin"; b.UpstreamBranch = Options.VisualStudioBranchName; });
            Log.Trace($"Creating branch took {watch.Elapsed.TotalSeconds} seconds");
        }

        private static void FetchLatest(Repository enlistment, FetchOptions fetchOptions)
        {
            var origin = enlistment.Network.Remotes["origin"];
            Log.Info($"Fetching from {origin.Url}");
            var watch = Stopwatch.StartNew();
            enlistment.Fetch(origin.Name, GetFetchOptions());
            Log.Trace($"Fetching took {watch.Elapsed.TotalSeconds} seconds");
        }

        private static string GetAbsolutePathForEnlistment() => Path.GetFullPath(Options.EnlistmentPath);

        private static CheckoutOptions GetCheckoutOptions()
        {
            Log.Trace("Getting checkout options");
            return new CheckoutOptions
            {
                CheckoutModifiers = CheckoutModifiers.Force,
                CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict,
            };
        }

        private static Credentials GetCredentials()
        {
            Log.Trace("Getting fetch options");
            return new UsernamePasswordCredentials
            {
                Username = Options.Username,
                Password = Options.Password
            };
        }

        private static FetchOptions GetFetchOptions()
        {
            Log.Trace("Getting fetch options");
            return new FetchOptions
            {
                CredentialsProvider = (url, usernameFromUrl, types) => GetCredentials(),
                Prune = true,
            };
        }

        private static Branch GetLatestAndCreateBranch(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Log.Trace($"Loading git enlistment at {Options.EnlistmentPath}");
            var enlistment = Enlistment;
            var fetchOptions = GetFetchOptions();
            return CreateBranch(enlistment, fetchOptions, cancellationToken);
        }

        private static PushOptions GetPushOptions()
        {
            Log.Trace("Getting push options");
            return new PushOptions
            {
                CredentialsProvider = (url, usernameFromUrl, types) => GetCredentials()
            };
        }

        private static StageOptions GetStageOptions()
        {
            return new StageOptions
            {
                ExplicitPathsOptions = new ExplicitPathsOptions
                {
                    OnUnmatchedPath = (s) => Log.Error($"Path could not be matched '{s}'"),
                    ShouldFailOnUnmatchedPath = true
                },
                IncludeIgnored = true
            };
        }

        private static Branch SwitchToBranchAndUpdate(string branchToSwitchTo, string baseBranchName)
        {
            if (!baseBranchName.StartsWith("origin/"))
            {
                baseBranchName = "origin/" + baseBranchName;
            }

            FetchLatest(Enlistment, GetFetchOptions());
            Enlistment.Checkout(branchToSwitchTo, GetCheckoutOptions());
            var baseBranch = Enlistment.Branches.Single(b => b.FriendlyName == baseBranchName);
            Enlistment.Reset(ResetMode.Hard, baseBranch.Tip);
            var branch = Enlistment.Head;
            return branch;
        }

        private static void CreateDummyCommit(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = $"DUMMY INSERTION FOR {Options.InsertionName}";
            var options = new CommitOptions()
            {
                AllowEmptyCommit = true
            };
            var watch = Stopwatch.StartNew();
            var commit = Enlistment.Commit(
                message,
                InsertionToolSignature,
                InsertionToolSignature,
                options);
            Log.Trace($"Committing took {watch.Elapsed.TotalSeconds} seconds");
        }

        private static Branch PushChanges(Branch branch, BuildVersion newRoslynVersion, CancellationToken cancellationToken, bool forcePush = false)
        {
            StageFiles(newRoslynVersion, cancellationToken);
            CommitStagedChanges(newRoslynVersion, cancellationToken);
            return PushChanges(branch, cancellationToken, forcePush: forcePush);
        }

        private static Branch PushChanges(Branch branch, CancellationToken cancellationToken, bool forcePush = false)
        {
            Stopwatch watch;
            cancellationToken.ThrowIfCancellationRequested();
            Log.Info($"Pushing branch");
            watch = Stopwatch.StartNew();
            var destinationSpec = Enlistment.Refs["HEAD"].TargetIdentifier;
            if (forcePush)
            {
                destinationSpec = "+" + destinationSpec;
            }

            Enlistment.Network.Push(Enlistment.Network.Remotes["origin"], destinationSpec, branch.CanonicalName, GetPushOptions());
            Log.Trace($"Pushing took {watch.Elapsed.TotalSeconds} seconds");
            return Enlistment.Head;
        }

        private static void StageFiles(BuildVersion newRoslynVersion,CancellationToken cancellationToken)
        {
            var repositoryStatus = Enlistment.RetrieveStatus();
            if (repositoryStatus.IsDirty)
            {
                var filesToStage = repositoryStatus
                    .Where(item => item.State != FileStatus.Unaltered && item.State != FileStatus.Ignored)
                    .Select(item => item.FilePath);

                cancellationToken.ThrowIfCancellationRequested();
                Log.Info($"Staging {filesToStage.Count()} file(s)");
                var watch = Stopwatch.StartNew();
                Enlistment.Stage(filesToStage, GetStageOptions());
                Log.Trace($"Staging took {watch.Elapsed.TotalSeconds} seconds");
            }
        }
    }
}
