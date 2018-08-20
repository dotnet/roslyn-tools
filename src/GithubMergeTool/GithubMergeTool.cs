// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GithubMergeTool
{
    public class GithubMergeTool
    {
        private static readonly Uri GithubBaseUri = new Uri("https://api.github.com/");

        private readonly HttpClient _client;

        public GithubMergeTool(
            string username,
            string password)
        {
            var client = new HttpClient();
            client.BaseAddress = GithubBaseUri;

            var authArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(authArray));
            client.DefaultRequestHeaders.Add(
                "user-agent",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");

            _client = client;
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
            string srcBranch,
            string destBranch,
            bool addAutoMergeLabel = false)
        {
            string prTitle = $"Merge {srcBranch} to {destBranch}";
            string prBranchName = $"merges/{srcBranch}-to-{destBranch}";

            // Check to see if there's already a PR
            HttpResponseMessage prsResponse = await _client.GetAsync(
                $"repos/{repoOwner}/{repoName}/pulls?state=open&base={destBranch}&head={repoOwner}:{prBranchName}");
            if (!prsResponse.IsSuccessStatusCode)
            {
                return (false, prsResponse);
            }

            var prsBody = JArray.Parse(await prsResponse.Content.ReadAsStringAsync());
            if (prsBody.Any(pr => (string)pr["title"] == prTitle))
            {
                return (false, null);
            }

            // Get the SHA for the source branch
            var response = await _client.GetAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{srcBranch}");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return (false, response);
            }

            var jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());
            if (jsonBody.Type == JTokenType.Array)
            {
                // Branch doesn't exist
                return (false, response);
            }

            var srcSha = ((JValue)jsonBody["object"]["sha"]).ToObject<string>();

            // Create a branch on the repo
            var body = $@"
{{
    ""ref"": ""refs/heads/{prBranchName}"",
    ""sha"": ""{srcSha}""
}}";

            Console.WriteLine("Creating branch");
            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/git/refs", body);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                // Branch already exists. Hard reset to the new SHA
                if (response.StatusCode == (HttpStatusCode)422)
                {
                    Console.WriteLine($"Resetting branch {prBranchName}");
                    response = await _client.PostAsyncAsJson(
                        $"repos/{repoOwner}/{repoName}/git/refs/heads/{prBranchName}",
                        $@"
{{
    ""sha"": ""{srcSha}"",
    ""force"": true
}}");
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

            const string newLine = @"
";

            var prMessage = $@"
This is an automatically generated pull request from {srcBranch} into {destBranch}.

``` bash
git fetch --all
git checkout {prBranchName}
git reset --hard upstream/{destBranch}
git merge upstream/{srcBranch}
# Fix merge conflicts
git commit
git push {prBranchName} --force
```

Once all conflicts are resolved and all the tests pass, you are free to merge the pull request.";

            prMessage = prMessage.Replace(newLine, "\\n");

            // Create a PR from the new branch to the dest
            body = $@"
{{
    ""title"": ""{prTitle}"",
    ""body"": ""{prMessage}"",
    ""head"": ""{prBranchName}"",
    ""base"": ""{destBranch}""
}}";

            Console.WriteLine("Creating PR");
            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/pulls", body);

            // 422 (Unprocessable Entity) indicates there were no commits to merge
            if (response.StatusCode == (HttpStatusCode)422)
            {
                // Delete the pr branch if the PR was not created.
                await _client.DeleteAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{prBranchName}");
                return (false, null);
            }

            jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());

            var prNumber = (string)jsonBody["number"];

            // Add labels to the issue
            body = $"[ \"Area-Infrastructure\"{(addAutoMergeLabel ? $", \"{AutoMergeLabelText}\"" : "")} ]";
            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/issues/{prNumber}/labels", body);

            if (!response.IsSuccessStatusCode)
            {
                return (true, response);
            }

            return (true, null);
        }

        public const string AutoMergeLabelText = "Auto-Merge If Tests Pass";

        /// <summary>
        /// Fetch the list of PRs that have been marked with the <see cref="AutoMergeLabelText"/>
        /// label.
        /// </summary>
        public async Task<(IEnumerable<string> prs, HttpResponseMessage error)> FetchAutoMergeablePrs(
            string repoOwner,
            string repoName)
        {
            var uriLabelText = AutoMergeLabelText.Replace(" ", "%20");
            var requestUri = $"repos/{repoOwner}/{repoName}/issues?state=open&labels={uriLabelText}";
            HttpResponseMessage response = await _client.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                return (Array.Empty<string>(), response);
            }

            var body = JArray.Parse(await response.Content.ReadAsStringAsync());
            return (body.Select(pr => (string)pr["number"]), null);
        }

        /// <summary>
        /// If the given PR meets all requirements for auto-merge, merge the PR.
        /// </summary>
        /// <returns>
        /// (true, null) if the PR was merged succesfully
        /// (false, null) if the PR was not merged because it did not meet requirements
        /// (false, error object) if an error was encountered while trying to merge the PR
        /// </returns>
        public async Task<(bool merged, HttpResponseMessage error)> MergeAutoMergeablePr(
            string repoOwner,
            string repoName,
            string prId)
        {
            var prUri = $"repos/{repoOwner}/{repoName}/pulls/{prId}";
            var prResponse = await _client.GetAsync(prUri);
            if (!prResponse.IsSuccessStatusCode)
            {
                return (false, prResponse);
            }

            var prInfo = JObject.Parse(await prResponse.Content.ReadAsStringAsync());
            if ((string)prInfo["state"] != "open" ||
                ((string)prInfo["mergeable"]).ToLower() != "true")
            {
                return (false, null);
            }

            // Check that the PR is by 'dotnet-bot'. Eventually we will support created PRs
            // created by other users, but not right now.
            if ((string)prInfo["user"]["login"] != "dotnet-bot")
            {
                return (false, null);
            }

            // Check that the PR has a well-known 'auto-merge' label
            var prIssueResponse = await _client.GetAsync($"repos/{repoOwner}/{repoName}/issues/{prId}");
            if (!prIssueResponse.IsSuccessStatusCode)
            {
                return (false, prIssueResponse);
            }

            var prIssueBody = JObject.Parse(await prIssueResponse.Content.ReadAsStringAsync());
            if (!prIssueBody["labels"].Any(label => (string)label["name"] == AutoMergeLabelText))
            {
                return (false, null);
            }

            // Check that the PR has no rejections
            var reviewsResponse = await _client.GetAsync(prUri + "/reviews");
            if (!reviewsResponse.IsSuccessStatusCode)
            {
                return (false, reviewsResponse);
            }

            var prReviews = JArray.Parse(await reviewsResponse.Content.ReadAsStringAsync());
            if (!prReviews.All(review => (string)review["state"] != "CHANGES_REQUESTED"))
            {
                return (false, reviewsResponse);
            }

            // Check that there are no failing required tests
            var mergeBranchRef = (string)prInfo["head"]["ref"];
            var baseBranchRef = (string)prInfo["base"]["ref"];

            var branchResponse = await _client.GetAsync($"/repos/{repoOwner}/{repoName}/branches/{baseBranchRef}");
            if (!branchResponse.IsSuccessStatusCode)
            {
                return (false, branchResponse);
            }

            var branchInfo = JObject.Parse(await branchResponse.Content.ReadAsStringAsync());
            IEnumerable<string> requiredTests = branchInfo["protection"]["required_status_checks"]["contexts"].Values<string>();

            var testStatusResponse = await _client.GetAsync($"repos/{repoOwner}/{repoName}/commits/{mergeBranchRef}/status");
            if (!testStatusResponse.IsSuccessStatusCode)
            {
                return (false, testStatusResponse);
            }

            var testStatusBody = JObject.Parse(await testStatusResponse.Content.ReadAsStringAsync());
            var statusDict = testStatusBody["statuses"].Select(t => ((string)t["context"], "success" == (string)t["state"]))
                .ToDictionary(t => t.Item1, t => t.Item2);

            // If there are no required tests, treat *any* test failure as a blocker
            if (!requiredTests.Any() && statusDict.Any(kvp => !kvp.Value))
            {
                // There is a failing non-required test
                return (false, null);
            }
            else
            {
                foreach (var test in requiredTests)
                {
                    if (!statusDict.ContainsKey(test) || !statusDict[test])
                    {
                        // There is a failing required test
                        return (false, null);
                    }
                }
            }

            // Check if there are any approvals
            if (!prReviews.Any(review => (string)review["state"] == "APPROVED"))
            {
                // If not, mark the PR as approved
                var reviewBody = $@"
{{
    ""body"": ""Auto-approval"",
    ""event"": ""APPROVE""
}}";
                var approveResponse = await _client.PostAsyncAsJson(
                    $"repos/{repoOwner}/{repoName}/pulls/{prId}/reviews",
                    reviewBody);
                if (!approveResponse.IsSuccessStatusCode)
                {
                    return (false, approveResponse);
                }
            }

            // Merge the PR
            var mergeSha = (string)prInfo["head"]["sha"];
            var mergeBody = $@"
{{
    ""sha"": ""{mergeSha}""
}}";
            var mergeResponse = await _client.PutAsyncAsJson(prUri + "/merge", mergeBody);
            if (!mergeResponse.IsSuccessStatusCode)
            {
                return (false, mergeResponse);
            }

            // Delete the branch
            // Ignore failure if we couldn't
            _ = await _client.DeleteAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{mergeBranchRef}");

            return (true, null);
        }
    }
}
