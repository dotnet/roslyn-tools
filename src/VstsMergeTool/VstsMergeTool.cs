// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.TeamFoundation.SourceControl.WebApi;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VstsMergeTool
{
    public class VstsMergeTool
    {
        private static Logger Logger;

        private const int Timeout = 1000 * 60;

        private readonly GitHttpClient GitHttpClient;

        private List<GitRef> RefsInfo;

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
            this.GitHttpClient = gitHttpClient;
            Logger = LogManager.GetCurrentClassLogger();

            var source = sourceBranch.Split('/');
            SourceName = source[source.Length - 1];

            var dest = destBranch.Split('/');
            DestName = dest[dest.Length - 1];

            this.SourceBranch = sourceBranch;
            this.DestBranch = destBranch;
            
            this.DummyBranchName = $"refs/heads/merge/{SourceName}-to-{DestName}";
            this.Cts = new CancellationTokenSource();
        }

        public async Task<bool> CreatePullRequest()
        {
            Cts.CancelAfter(Timeout);
            // Fetch the repository id according to repository name
            await GetRepositoryId(Settings.RepositoryName, Cts.Token);

            var getCurrentBranchInfo = await GetBranchInfoAsync(Cts.Token);

            if (!getCurrentBranchInfo)
            {
                Logger.Error($"Fail to get the branch and PR information about {Settings.TFSProjectName}");
                return false;
            }

            // Check if source branch and target branch both exist
            if (!IsBranchExist(SourceBranch) || !IsBranchExist(DestBranch))
            {
                Logger.Error($"{SourceBranch} or {DestBranch} doesn't exist");
                return false;
            }

            // Check if there are existing dummybranch and open PR
            var isDummyBranchAndOpenPrExisting = await IsDummyBranchAndOpenPrExists(Cts.Token);

            if (isDummyBranchAndOpenPrExisting)
            {
                Logger.Warn("Previous auto merge is not finshed");
                return false;
            }

            // Create a dummy branch 
            bool branchCreated = await CreateNewBranch(Cts.Token);

            Logger.Info("Dummy branch is created");

            if (!branchCreated)
            {
                Logger.Error("Fail to create dummy branch");
                return false;
            }

            // Create PR and get its id
            bool prCreated = await CreateNewPullRequest(DummyBranchName, Cts.Token);

            if (!prCreated)
            {
                Logger.Error("Fail to create new pull request");
                return false;
            }

            Logger.Info("Pull Request is created");

            // TODO: 1. If there is no conflict, let source branch merge to dummy branch.
            //       2. If conflict existing, stop. When conflict is resolve, then merge.
            return true;
        }

        private bool IsBranchExist(string branchName)
        {
            var existingRefs = RefsInfo.Select(refs => refs.Name);
            return existingRefs.Contains(branchName);
        }

        private async Task<bool> GetBranchInfoAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                // Download the current Branch and pull Request information about this repo            
                var response = await GitHttpClient.GetRefsAsync(
                                project: Settings.TFSProjectName,
                                repositoryId:RepositoryId,
                                cancellationToken: token);

                RefsInfo = response;
                return true;
            }
            catch (OperationCanceledException)
            {
                Logger.Info($"Time out occurs during downloading {Settings.TFSProjectName} branch and pull request information");
                throw;
            }
            catch (Exception e)
            {
                Logger.Info($"Exception occurs during downloading {Settings.TFSProjectName} branch and pull request information, message: {e.Message}");
                throw;
            }
        }

        private async Task<bool> IsDummyBranchAndOpenPrExists(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (RefsInfo.Where(info => info.Name == DummyBranchName).Any())
            {
                // If the dummy branch exists, check if there is open PR exist
                var searchCriteria = new GitPullRequestSearchCriteria()
                {
                    RepositoryId = this.RepositoryId,
                    Status = PullRequestStatus.Active,
                    TargetRefName = DestBranch,
                    SourceRefName = DummyBranchName,
                };

                try
                {
                    var response = await GitHttpClient.GetPullRequestsByProjectAsync(Settings.TFSProjectName, searchCriteria, cancellationToken: token);
                    if (response.Count != 0)
                    {
                        // If there is an open PR, means the preious merge is not finished
                        Logger.Info($"There are existing pull requests between {DummyBranchName} and {DestBranch}");
                        return true;
                    }
                    else
                    {
                        // If there is no open PR, delete the dummy branch because we are going to create a new PR
                        if (await RemoveBranch(DummyBranchName, token))
                        {
                            return false;
                        }
                        else
                        {
                            Logger.Error("Fail to delete old dummy branch.");
                            return true;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Error("Time out occurs when try to check whether dummy branch and open PR already exist");
                    throw;
                }
                catch (Exception e)
                {
                    Logger.Error($"Exception occurs when try to check whether dummy branch and open PR already exist, message: {e.Message}");
                    throw;
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
                SourceRefName = SourceBranch,
                TargetRefName = dummyBranchName,
            };
            try
            {
                var response = await GitHttpClient.CreatePullRequestAsync(pullRequest, RepositoryId);
                Logger.Info($"Pull Request ID: {response.PullRequestId}, URL: {response.Url}");
                AutoPullRequestId = response.PullRequestId;
                return true;
            }
            catch(OperationCanceledException)
            {
                Logger.Error($"Time out occurs when try to create PR from {SourceBranch} to {dummyBranchName}");
                throw;
            }
            catch(Exception e)
            {
                Logger.Error($"Exception occurs when try to create PR from {SourceBranch} to {dummyBranchName}, message: {e.Message}");
                throw;
            }
        }

        private async Task<bool> CreateNewBranch(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // Get the sha1 of destBranch
            var query = RefsInfo.Select(refs => (refs.Name, refs.ObjectId)).Where(refsTuple => refsTuple.Name == DestBranch)
                .Select(refsTuple => refsTuple.ObjectId);

            var sha1 = query.ToList().First();

            var refUpdate = new List<GitRefUpdate>() { new GitRefUpdate() { IsLocked = false, OldObjectId = new string('0', 40), NewObjectId = sha1, Name = DummyBranchName } };

            try
            {
                var response = await GitHttpClient.UpdateRefsAsync(refUpdate, RepositoryId);
                if (response.Where(res => !res.Success).Any())
                {
                    Logger.Error("Fail to create a new branch");
                    return false;
                }
                return true;
            }
            catch(OperationCanceledException)
            {
                Logger.Error($"Time out occurs when try to create {DummyBranchName}");
                throw;
            }
            catch(Exception e)
            {
                Logger.Error($"Exception occurs when try to create {DummyBranchName}, message: {e.Message}");
                throw;
            }
        }

        private async Task<bool> GetRepositoryId(string repoName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // Some request don't accept Repository name as parameter. Therefore, just use repository id.
            Logger.Info($"Trying to get {repoName}' id");

            try
            {
                var response = await GitHttpClient.GetRepositoryAsync(Settings.TFSProjectName, RepositoryId);
                this.RepositoryId = response.Id;
                return true;
            }
            catch(OperationCanceledException)
            {
                Logger.Error($"Time out occurs when try to get the ID of {repoName}");
                throw;
            }
            catch(Exception e)
            {
                Logger.Error($"Exception occurs when try to get the ID of {repoName}, message: {e.Message}");
                throw;
            }
        } 

        private async Task<bool> RemoveBranch(string branchName, CancellationToken token)
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
            Logger.Info($"Trying to delete {branchName}");

            try
            {
                var response = await GitHttpClient.UpdateRefsAsync(refDelete, RepositoryId);
                if (response.Where(res => !res.Success).Any())
                {
                    Logger.Error($"Fail to delete the {branchName}");
                    return false;
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                Logger.Error($"Time out occurs when try to delete {branchName}");
                throw;
            }
            catch (Exception e)
            {
                Logger.Error($"Exception occurs when try to delete {branchName}, message: {e.Message}");
                throw;
            }
        }
    }
}
