using Microsoft.TeamFoundation.SourceControl.WebApi;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace VstsMergeTool
{
    public class VstsMergeTool
    {
        private static Logger logger;

        private Options Options { get; }

        private readonly GitHttpClient gitHttpClient;

        private List<GitRef> RefsInfo;

        private readonly string SourceName;

        private readonly string DestName;

        public VstsMergeTool(Options options, GitHttpClient gitHttpClient)
        {
            Options = options;
            this.gitHttpClient = gitHttpClient;
            logger = LogManager.GetCurrentClassLogger();

            var source = Options.SourceBranch.Split('/');
            SourceName = source[source.Length - 1];

            var dest = Options.DestBranch.Split('/');
            DestName = dest[dest.Length - 1];
        }

        public async Task<bool> CreatePullRequest()
        {
            var getCurrentBranchInfo = await GetBranchInfoAsync();

            if (!getCurrentBranchInfo)
            {
                return false;
            }

            // Check if source branch and target branch both exist
            if (!IsBranchExist(Options.SourceBranch) || !IsBranchExist(Options.DestBranch))
            {
                logger.Error($"{Options.SourceBranch} or {Options.DestBranch} doesn't exist");
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
            var response = await gitHttpClient.GetRefsAsync(project: Options.Project, repositoryId: Options.RepoId);
            RefsInfo = response;

            return true;
        }

        private async Task<bool> CheckWhetherPrExisitingAsync()
        {
            var searchCriteria = new GitPullRequestSearchCriteria()
            {
                RepositoryId = new Guid(Options.RepoId),
                Status = PullRequestStatus.Active,
                TargetRefName = Options.DestBranch,
                SourceRefName = Options.SourceBranch,
            };
            var response = await gitHttpClient.GetPullRequestsByProjectAsync(Options.Project, searchCriteria);

            if (response.Count != 0)
            {
                logger.Error($"There are existing pull requests between {Options.SourceBranch} and {Options.DestBranch}");
                return true;
            }

            return false;
        }


        private async Task<int> CreateNewPullRequest(string dummyBranchName)
        {
            var pullRequest = new GitPullRequest()
            {
                Title = $"Auto PR from {SourceName} to {DestName}",
                SourceRefName = Options.SourceBranch,
                TargetRefName = dummyBranchName,
<<<<<<< HEAD
                // TODO : Add reviewer info
=======
>>>>>>> 950453b0c86ea644b7ef43d90cc746b49f512d41
            };
            var response = await gitHttpClient.CreatePullRequestAsync(pullRequest, Options.RepoId);

            return response.PullRequestId;
        }

        private async Task<(bool Succeed, string BranchName)> CreateNewBranch()
        {
            // Get the sha1 of destBranch
            var query = RefsInfo.Select(refs => (refs.Name, refs.ObjectId)).Where(refsTuple => refsTuple.Name == Options.DestBranch)
                .Select(refsTuple => refsTuple.ObjectId);

            var sha1 = query.ToList()[0];

            string branchName = $"refs/heads/dev/shech/dummyBranchFrom_{SourceName}_to_{DestName}_on_{DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss")}";

            var refUpdate = new List<GitRefUpdate>() { new GitRefUpdate() { IsLocked = false, OldObjectId = new string('0', 40), NewObjectId = sha1, Name = branchName } };

            var response = await gitHttpClient.UpdateRefsAsync(refUpdate, Options.RepoId);

            if (response.Where(res => !res.Success).Any())
            {
                logger.Error("Fail to create a new branch");
                return (false, branchName);
            }
            return (true, branchName);
        }
    }
}
