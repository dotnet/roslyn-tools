// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VstsMergeTool
{
    public class VstsMergeTool
    {
        private const int Timeout = 1000 * 60;

        private readonly GitHttpClient gitHttpClient;

        private Settings Settings = Settings.Default;

        private readonly string SourceName;

        private readonly string DestName;

        private readonly string SourceBranch;

        private readonly string DestBranch;

        private readonly string DummyBranchName;

        private int AutoPullRequestId;

        private Guid RepositoryId;

        private CancellationTokenSource Cts;

        public VstsMergeTool(GitHttpClient gitHttpClient, string sourceBranch, string destBranch)
        {
            this.gitHttpClient = gitHttpClient;

            var source = sourceBranch.Split('/');
            SourceName = source[source.Length - 1];

            var dest = destBranch.Split('/');
            DestName = dest[dest.Length - 1];

            this.SourceBranch = sourceBranch.StartsWith("refs/heads/") ? sourceBranch : $"refs/heads/{sourceBranch}";
            this.DestBranch = destBranch.StartsWith("refs/heads/") ? destBranch : $"refs/heads/{destBranch}";

            this.DummyBranchName = $"refs/heads/merge/{SourceName}-to-{DestName}";
            this.Cts = new CancellationTokenSource();
        }

        public async Task<(bool isPrCreated, string message)> CreatePullRequest()
        {
            try
            {
                Cts.CancelAfter(Timeout);
                // Fetch the repository id according to repository name
                await GetRepositoryId(Settings.RepositoryName, Cts.Token);

                var branchInfo = await GetBranchInfoAsync(Cts.Token);
                var existingBranchNames = branchInfo.Select(b => b.Name);

                this.CheckBranchExists(existingBranchNames);

                // Check if there are existing dummybranch and open PR
                var isDummyBranchAndOpenPrExisting = await DoesDummyBranchAndOpenPrExist(existingBranchNames, Cts.Token);

                if (isDummyBranchAndOpenPrExisting)
                {
                    return (false, "Previous auto merge is still in progress.");
                }

                if (!IsMergeRequired(branchInfo))
                {
                    return (false, null);
                }

                // Create a dummy branch 
                await CreateNewBranch(branchInfo, Cts.Token);

                // Create PR and get its id
                bool prCreated = await CreateNewPullRequest(DummyBranchName, Cts.Token);

                if (!prCreated)
                {
                    return (false, "Fail to create new pull request");
                }

                Console.WriteLine("Pull Request is created");

                // TODO: 1. If there is no conflict, let source branch merge to dummy branch.
                //       2. If conflict existing, stop. When conflict is resolve, then merge.
                return (true, null);
            }
            catch (OperationCanceledException ex)
            {
                return (false, $"Timed out waiting for an operation: {ex.ToString()}");
            }
            catch (Exception ex)
            {
                // Gracefully exit.
                return (false, ex.ToString());
            }
        }

        private void CheckBranchExists(IEnumerable<string> branchName)
        {
            if (!branchName.Contains(SourceBranch))
            {
                throw new ArgumentException($"{SourceBranch} does not exist.");
            }

            if (!branchName.Contains(DestBranch))
            {
                throw new ArgumentException($"{DestBranch} does not exist.");
            }
        }

        private async Task<List<GitRef>> GetBranchInfoAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Download the current Branch and pull Request information about this repo            
            return await gitHttpClient.GetRefsAsync(
                            project: Settings.TFSProjectName,
                            repositoryId: RepositoryId,
                            cancellationToken: token);
        }

        private async Task<bool> DoesDummyBranchAndOpenPrExist(IEnumerable<string> branchNames, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (branchNames.Contains(DummyBranchName))
            {
                // If the dummy branch exists, check if there is open PR exist
                var searchCriteria = new GitPullRequestSearchCriteria()
                {
                    RepositoryId = this.RepositoryId,
                    Status = PullRequestStatus.Active,
                    TargetRefName = DestBranch,
                    SourceRefName = DummyBranchName,
                };

                var response = await gitHttpClient.GetPullRequestsByProjectAsync(Settings.TFSProjectName, searchCriteria, cancellationToken: token);
                if (response.Count != 0)
                {
                    // If there is an open PR, means the preious merge is not finished
                    Console.WriteLine($"There are existing pull requests between {DummyBranchName} and {DestBranch}");
                    return true;
                }
                else
                {
                    // If there is no open PR, delete the dummy branch because we are going to create a new PR
                    if (await TryRemoveBranch(DummyBranchName, token))
                    {
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Failed to delete old dummy branch.");
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> CreateNewPullRequest(string dummyBranchName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var pullRequest = new GitPullRequest()
            {
                Title = $"AutoMerge PR from {SourceName} to {DestName}",
                SourceRefName = dummyBranchName,
                TargetRefName = DestBranch,
            };

            Console.WriteLine($"Creating a new Pull Request: {pullRequest.Title} on \"{dummyBranchName}\"");

            var response = await gitHttpClient.CreatePullRequestAsync(pullRequest, RepositoryId);
            Console.WriteLine($"Pull Request ID: {response.PullRequestId}, URL: {response.Url}");
            AutoPullRequestId = response.PullRequestId;
            return true;
        }

        private async Task CreateNewBranch(IEnumerable<GitRef> branchInfo, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            Console.WriteLine($"Creating New Branch \"{DummyBranchName}\"");
            // Get the sha1 of the source branch
            var sourceBranchSha = branchInfo.Where(branch => string.Equals(branch.Name, SourceBranch)).Select(branch => branch.ObjectId).FirstOrDefault();

            var refUpdate = new List<GitRefUpdate>() { new GitRefUpdate() { IsLocked = false, OldObjectId = new string('0', 40), NewObjectId = sourceBranchSha, Name = DummyBranchName } };

            var response = await gitHttpClient.UpdateRefsAsync(refUpdate, RepositoryId);
            if (response.Where(res => !res.Success).Any())
            {
                throw new Exception("Fail to create a new branch");
            }
        }

        private bool IsMergeRequired(IEnumerable<GitRef> branchInfo)
        {
            var sourceBranchSha = branchInfo.Where(branch => string.Equals(branch.Name, SourceBranch)).Select(branch => branch.ObjectId).FirstOrDefault();
            var destBranchSha = branchInfo.Where(branch => string.Equals(branch.Name, DestBranch)).Select(branch => branch.ObjectId).FirstOrDefault();

            return !string.Equals(sourceBranchSha, destBranchSha);
        }

        private async Task GetRepositoryId(string repoName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // Some request don't accept Repository name as parameter. Therefore, just use repository id.
            Console.WriteLine($"Trying to get {repoName}' id");

            var response = await gitHttpClient.GetRepositoriesAsync(Settings.TFSProjectName);
            this.RepositoryId = response.Where(repo => repo.Name == Settings.RepositoryName).First().Id;
        }

        private async Task<bool> TryRemoveBranch(string branchName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var refDelete = new List<GitRefUpdate>() {
                new GitRefUpdate()
                {
                    IsLocked = false,
                    Name = branchName,
                    OldObjectId = new string('0', 40),
                    NewObjectId = new string('0', 40)
                }
            };

            Console.WriteLine($"Trying to delete {branchName}");

            var response = await gitHttpClient.UpdateRefsAsync(refDelete, RepositoryId);
            return !response.Where(res => !res.Success).Any();
        }
    }
}
