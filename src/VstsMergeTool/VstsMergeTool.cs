// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.TeamFoundation.SourceControl.WebApi;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VstsMergeTool
{
    public class VstsMergeTool
    {
        private static Logger logger;

        private readonly GitHttpClient gitHttpClient;

        private List<GitRef> RefsInfo;

        private Settings settings = Settings.Default;

        private readonly string SourceName;

        private readonly string DestName;

        private readonly string SourceBranch;

        private readonly string DestBranch;
        public VstsMergeTool(GitHttpClient gitHttpClient, string sourceBranch, string destBranch)
        {
            this.gitHttpClient = gitHttpClient;
            logger = LogManager.GetCurrentClassLogger();

            var source = sourceBranch.Split('/');
            SourceName = source[source.Length - 1];

            var dest = destBranch.Split('/');
            DestName = dest[dest.Length - 1];

            this.SourceBranch = sourceBranch;
            this.DestBranch = destBranch;
        }

        public async Task<bool> CreatePullRequest()
        {
            var getCurrentBranchInfo = await GetBranchInfoAsync();

            if (!getCurrentBranchInfo)
            {
                return false;
            }

            // Check if source branch and target branch both exist
            if (!IsBranchExist(SourceBranch) || !IsBranchExist(DestBranch))
            {
                logger.Error($"{SourceBranch} or {DestBranch} doesn't exist");
                return false;
            }

            // Check if there are existing PRs between source branch and target branch
            var isPullRequestExisting = await CheckWhetherPrExisitingAsync();

            if (isPullRequestExisting)
            {
                return false;
            }

            // Create a dummy branch 
            (bool branchCreated, string dummyBranchName) = await CreateNewBranch();

            logger.Info("Dummy branch is created");

            if (!branchCreated)
            {
                return false;
            }

            // Create PR and get its id
            var prId = await CreateNewPullRequest(dummyBranchName);


            // TODO: 1. If there is no conflict, let source branch merge to dummy branch.
            //       2. If conflict existing, stop. When conflict is resolve, then merge.
            //       3. When all tasks finished, remove the dummy branch
            return true;
        }

        private bool IsBranchExist(string branchName)
        {
            var existingRefs = RefsInfo.Select(refs => refs.Name);
            return existingRefs.Contains(branchName);
        }

        private async Task<bool> GetBranchInfoAsync()
        {
            // Download the current Branch and pull Request information about this repo            
            var response = await gitHttpClient.GetRefsAsync(project: settings.TFSProjectName, repositoryId: settings.RepositoryID);
            RefsInfo = response;

            return true;
        }

        private async Task<bool> CheckWhetherPrExisitingAsync()
        {
            var searchCriteria = new GitPullRequestSearchCriteria()
            {
                RepositoryId = new Guid(settings.RepositoryID),
                Status = PullRequestStatus.Active,
                TargetRefName = DestBranch,
                SourceRefName = SourceBranch,
            };
            var response = await gitHttpClient.GetPullRequestsByProjectAsync(settings.TFSProjectName, searchCriteria);

            if (response.Count != 0)
            {
                logger.Error($"There are existing pull requests between {SourceBranch} and {DestBranch}");
                return true;
            }

            return false;
        }


        private async Task<int> CreateNewPullRequest(string dummyBranchName)
        {
            var pullRequest = new GitPullRequest()
            {
                Title = $"Auto PR from {SourceName} to {DestName}",
                SourceRefName = SourceBranch,
                TargetRefName = dummyBranchName,
                // TODO : Add reviewer info
            };
            var response = await gitHttpClient.CreatePullRequestAsync(pullRequest, settings.RepositoryID);

            return response.PullRequestId;
        }

        private async Task<(bool Succeed, string BranchName)> CreateNewBranch()
        {
            // Get the sha1 of destBranch
            var query = RefsInfo.Select(refs => (refs.Name, refs.ObjectId)).Where(refsTuple => refsTuple.Name == DestBranch)
                .Select(refsTuple => refsTuple.ObjectId);

            var sha1 = query.ToList()[0];

            string branchName = $"refs/heads/merge/{SourceName}-vs-deps-to-{DestName}-vs-deps";

            var refUpdate = new List<GitRefUpdate>() { new GitRefUpdate() { IsLocked = false, OldObjectId = new string('0', 40), NewObjectId = sha1, Name = branchName } };

            var response = await gitHttpClient.UpdateRefsAsync(refUpdate, settings.RepositoryID);

            if (response.Where(res => !res.Success).Any())
            {
                logger.Error("Fail to create a new branch");
                return (false, branchName);
            }
            return (true, branchName);
        }
    }
}
