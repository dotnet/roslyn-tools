// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GithubMergeTool
{
    public class GithubMergeTool
    {
        private static readonly Uri GithubBaseUri = new Uri("https://api.github.com/");

        private readonly IHttpClientDecorator _client;

        public GithubMergeTool(
            string username,
            string password,
            bool isDryRun)
        {
            var client = new HttpClient
            {
                BaseAddress = GithubBaseUri
            };

            var authArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(authArray));
            client.DefaultRequestHeaders.Add(
                "user-agent",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");

            // Needed to call the check-runs endpoint
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.antiope-preview+json"));

            if (isDryRun)
            {
                _client = new NoOpHttpClientDecorator(client);
            }
            else
            {
                _client = new HttpClientDecorator(client);
            }
        }

        /// <summary>
        /// Create a merge PR.
        /// </summary>
        /// <returns>
        /// (true, null) if the PR was created without error.
        /// (true, error) if the PR was created but there was a subsequent error
        /// (false, null) if the PR wasn't created due to a PR already existing
        /// or if the <paramref name="destBranch"/> contains all the commits
        /// from <paramref name="srcBranch"/>.
        /// (false, error response) if there was an error creating the PR.
        /// </returns>
        public async Task<(bool prCreated, HttpResponseMessage error)> CreateMergePr(
            string repoOwner,
            string repoName,
            List<string> prOwners,
            string srcBranch,
            string destBranch,
            bool updateExistingPr,
            bool addAutoMergeLabel,
            bool isAutoTriggered)
        {
            // Get the SHA for the source branch
            // https://developer.github.com/v3/git/refs/#get-a-single-reference
            var response = await _client.GetAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{srcBranch}");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return (false, response);
            }

            var sourceBranchData = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(),
                new
                {
                    @object = new { sha = "" }
                });

            var srcSha = sourceBranchData.@object.sha;

            string prTitle = $"Merge {srcBranch} to {destBranch}";
            string prBranchName = $"merges/{srcBranch}-to-{destBranch}";

            // Check to see if there's already a PR from source to destination branch
            // https://developer.github.com/v3/pulls/#list-pull-requests
            HttpResponseMessage prsResponse = await _client.GetAsync(
                $"repos/{repoOwner}/{repoName}/pulls?state=open&base={destBranch}&head={repoOwner}:{prBranchName}");

            if (!prsResponse.IsSuccessStatusCode)
            {
                return (false, prsResponse);
            }

            var existingPrData = JsonConvert.DeserializeAnonymousType(await prsResponse.Content.ReadAsStringAsync(),
                new[]
                {
                    new
                    {
                        title = "",
                        number = "",
                        head = new
                        {
                            sha = ""
                        }
                    }
                }).FirstOrDefault(pr => pr.title == prTitle);

            if (existingPrData != null)
            {
                if (updateExistingPr)
                {
                    // Get the SHA of the PR branch HEAD
                    var prSha = existingPrData.head.sha;
                    var existingPrNumber = existingPrData.number;

                    // Check for merge conflicts
                    var existingPrMergeable = await IsPrConflicted(existingPrNumber);

                    // Only update PR w/o merge conflicts
                    if (existingPrMergeable == true && prSha != srcSha)
                    {
                        // Try to reset the HEAD of PR branch to latest source branch
                        response = await ResetBranch(prBranchName, srcSha, force: false);
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"There's additional change in `{srcBranch}` but an attempt to fast-forward `{prBranchName}` failed.");
                            return (false, response);
                        }

                        await PostComment(existingPrNumber, $"Reset HEAD of `{prBranchName}` to `{srcSha}`");

                        // Check for merge conflicts again after reset.
                        existingPrMergeable = await IsPrConflicted(existingPrNumber);
                    }

                    // Add label if there's merge conflicts even if we made no change to merge branch,
                    // since can also be introduced by change in destination branch. It's no-op if the
                    // label already exists.
                    if (existingPrMergeable == false)
                    {
                        await AddLabels(existingPrNumber, new List<string> { MergeConflictsLabelText });
                    }
                }

                return (false, null);
            }

            Console.WriteLine("Creating branch");

            // Create a PR branch on the repo
            // https://developer.github.com/v3/git/refs/#create-a-reference
            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/git/refs", JsonConvert.SerializeObject(
                new
                {
                    @ref = $"refs/heads/{prBranchName}",
                    sha = srcSha
                }));

            if (response.StatusCode != HttpStatusCode.Created)
            {
                // PR branch already exists. Hard reset to the new SHA
                if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                {
                    response = await ResetBranch(prBranchName, srcSha, force: true);
                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, response);
                    }
                }
                else
                {
                    return (false, response);
                }
            }

            string autoTriggeredMessage = isAutoTriggered ? "" : $@"(created from a manual run of the PR generation tool)\n";

            var prMessage = $@"
This is an automatically generated pull request from {srcBranch} into {destBranch}.
{autoTriggeredMessage}
``` bash
git fetch --all
git checkout {prBranchName}
git reset --hard upstream/{destBranch}
git merge upstream/{srcBranch}
# Fix merge conflicts
git commit
git push upstream {prBranchName} --force
```
Once all conflicts are resolved and all the tests pass, you are free to merge the pull request.";

            Console.WriteLine("Creating PR");

            // Create a PR from the new branch to the dest
            // https://developer.github.com/v3/pulls/#create-a-pull-request
            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/pulls", JsonConvert.SerializeObject(
                new
                {
                    title = prTitle,
                    body = prMessage,
                    head = prBranchName,
                    @base = destBranch
                }));

            // 422 (Unprocessable Entity) indicates there were no commits to merge
            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                // Delete the pr branch if the PR was not created.
                // https://developer.github.com/v3/git/refs/#delete-a-reference
                await _client.DeleteAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{prBranchName}");
                return (false, null);
            }

            var createPrData = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
            {
                number = "",
                mergeable = (bool?)null
            });

            var prNumber = createPrData.number;
            var hasConflicts = !createPrData.mergeable;

            if (hasConflicts == null)
            {
                hasConflicts = await IsPrConflicted(prNumber);
            }

            var labels = new List<string> { AreaInfrastructureLabelText };
            if (addAutoMergeLabel)
            {
                labels.Add(AutoMergeLabelText);
            }

            if (hasConflicts == true)
            {
                Console.WriteLine("PR has merge conflicts. Adding Merge Conflicts label.");
                labels.Add(MergeConflictsLabelText);
            }

            // Add labels to the issue
            response = await AddLabels(prNumber, labels);

            // Add assignees to the issue
            if (prOwners.Any())
            {
                response = await AddAssignees(prNumber, prOwners);
            }

            if (!response.IsSuccessStatusCode)
            {
                return (true, response);
            }

            return (true, null);

            Task<HttpResponseMessage> ResetBranch(string branchName, string sha, bool force)
            {
                Console.WriteLine($"Resetting branch {branchName}");

                // https://developer.github.com/v3/git/refs/#update-a-reference
                var body = JsonConvert.SerializeObject(new { sha, force });
                return _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/git/refs/heads/{branchName}", body);
            }

            Task<HttpResponseMessage> PostComment(string prNumber, string comment)
            {
                // https://developer.github.com/v3/pulls/comments/#create-a-comment
                var body = JsonConvert.SerializeObject(new { body = comment });
                return _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/issues/{prNumber}/comments", body);
            }

            async Task<bool?> IsPrConflicted(string prNumber, int maxAttempts = 5)
            {
                var attempt = 0;
                bool? prHasConflicts = null;

                Console.Write("Waiting for mergeable status");
                while (prHasConflicts == null && attempt < maxAttempts)
                {
                    attempt++;
                    Console.Write(".");
                    await Task.Delay(1000);

                    // Get the pull request
                    // https://developer.github.com/v3/pulls/#get-a-single-pull-request
                    var response = await _client.GetAsync($"repos/{repoOwner}/{repoName}/pulls/{prNumber}");
                    var data = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
                    {
                        mergeable = (bool?)null,
                        mergeable_state = "",
                        labels = new[]
                        {
                            new { name = "" }
                        }
                    });

                    if (data.mergeable is null)
                    {
                        // GitHub is still computing the mergeability of this PR
                        continue;
                    }

                    // "dirty" indicated merge conflicts causing the mergeability to be false
                    // see https://github.community/t5/How-to-use-Git-and-GitHub/API-Getting-the-reason-that-a-pull-request-isn-t-mergeable/td-p/5796
                    var hasMergeConflicts = data.mergeable == false && data.mergeable_state == "dirty";

                    // treat the presense of a merge conflict label as unmergeable so that we do not
                    // update a corrected PR with new merge conflicts
                    var hasMergeConflictsLabel = data.labels.Select(label => label.name).Contains(MergeConflictsLabelText);

                    prHasConflicts = hasMergeConflicts || hasMergeConflictsLabel;
                }

                Console.WriteLine();

                if (prHasConflicts == null)
                {
                    Console.WriteLine($"##vso[task.logissue type=warning]Timed out waiting for PR mergeability status to become available.");
                }

                return prHasConflicts;
            }

            Task<HttpResponseMessage> AddLabels(string prNumber, List<string> labels)
            {
                // https://developer.github.com/v3/issues/labels/#add-labels-to-an-issue
                return _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/issues/{prNumber}/labels", JsonConvert.SerializeObject(labels));
            }

            Task<HttpResponseMessage> AddAssignees(string prNumber, List<string> assignees)
            {
                // https://developer.github.com/v3/issues/assignees/#add-assignees-to-an-issue
                return _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/issues/{prNumber}/assignees", JsonConvert.SerializeObject(new { assignees }));
            }
        }

        public const string AutoMergeLabelText = "auto-merge";
        public const string MergeConflictsLabelText = "Merge Conflicts";
        public const string AreaInfrastructureLabelText = "Area-Infrastructure";
    }
}
