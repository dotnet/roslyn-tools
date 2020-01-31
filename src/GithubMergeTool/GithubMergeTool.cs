// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            string srcBranch,
            string destBranch,
            bool updateExistingPr,
            bool addAutoMergeLabel,
            bool isAutoTriggered)
        {
            string prTitle = $"Merge {srcBranch} to {destBranch}";
            string prBranchName = $"merges/{srcBranch}-to-{destBranch}";

            // Check to see if there's already a PR from source to destination branch
            HttpResponseMessage prsResponse = await _client.GetAsync(
                $"repos/{repoOwner}/{repoName}/pulls?state=open&base={destBranch}&head={repoOwner}:{prBranchName}");

            if (!prsResponse.IsSuccessStatusCode)
            {
                return (false, prsResponse);
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
                // Source branch doesn't exist
                return (false, response);
            }

            var srcSha = ((JValue)jsonBody["object"]["sha"]).ToObject<string>();

            var existingPR = JArray.Parse(await prsResponse.Content.ReadAsStringAsync()).FirstOrDefault(pr => (string)pr["title"] == prTitle);
            if (existingPR != null)
            {
                if (updateExistingPr)
                {
                    // Get the SHA of the PR branch HEAD
                    var prSha = ((JValue)existingPR["head"]["sha"]).ToObject<string>();
                    var existingPrNumber = ((JValue)existingPR["number"]).ToObject<string>();

                    if (prSha != srcSha)
                    {
                        // Check if there's "merge conflicts" tag on PR, it so, we don't update
                        response = await _client.GetAsync($"repos/{repoOwner}/{repoName}/issues/{existingPrNumber}");

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            return (false, response);
                        }

                        jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());
                        if (((JArray)jsonBody["labels"]).Any(label => (string)label["name"] == MergeConflictsLabelText))
                        {
                            return (false, null);
                        }

                        // Reset the HEAD of PR branch to latest source branch
                        response = await ResetBranch(prBranchName, srcSha);
                        if (!response.IsSuccessStatusCode)
                        {
                            return (false, response);
                        }

                        // Post a comment to the PR
                        var commentBody = $@"
{{
    ""body"": ""Reset HEAD of {prBranchName} to {srcSha}""
}}";
                        await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/issues/{existingPrNumber}/comments", commentBody);
                    }
                }

                return (false, null);
            }

            // Create a PR branch on the repo
            var body = $@"
{{
    ""ref"": ""refs/heads/{prBranchName}"",
    ""sha"": ""{srcSha}""
}}";

            Console.WriteLine("Creating branch");

            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/git/refs", body);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                // PR branch already exists. Hard reset to the new SHA
                if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                {
                    response = await ResetBranch(prBranchName, srcSha);
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
            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                // Delete the pr branch if the PR was not created.
                await _client.DeleteAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{prBranchName}");
                return (false, null);
            }

            jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());

            var prNumber = (string)jsonBody["number"];
            var mergeable = (bool?)jsonBody["mergeable"];
            if (mergeable == null)
            {
                const int maxAttempts = 5;
                var attempt = 0;

                Console.Write("Waiting for mergeable status");
                while (mergeable == null && attempt < maxAttempts)
                {
                    attempt++;
                    Console.Write(".");
                    await Task.Delay(1000);
                    response = await _client.GetAsync($"repos/{repoOwner}/{repoName}/pulls/{prNumber}");
                    jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());
                    mergeable = (bool?)jsonBody["mergeable"];
                }

                Console.WriteLine();

                if (attempt == maxAttempts)
                {
                    Console.WriteLine($"##vso[task.logissue type=warning]Timed out waiting for PR mergeability status to become available.");
                }
            }


            var labels = new List<string> { "Area-Infrastructure" };
            if (addAutoMergeLabel)
            {
                labels.Add(AutoMergeLabelText);
            }

            if (mergeable == false)
            {
                Console.WriteLine("PR has merge conflicts. Adding Merge Conflicts label.");
                labels.Add(MergeConflictsLabelText);
            }

            // Add labels to the issue
            body = JsonConvert.SerializeObject(labels);
            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/issues/{prNumber}/labels", body);

            if (!response.IsSuccessStatusCode)
            {
                return (true, response);
            }

            return (true, null);

            Task<HttpResponseMessage> ResetBranch(string branchName, string sha)
            {
                Console.WriteLine($"Resetting branch {branchName}");
                return _client.PostAsyncAsJson(
                    $"repos/{repoOwner}/{repoName}/git/refs/heads/{branchName}",
                    $@"
{{
    ""sha"": ""{sha}"",
    ""force"": true
}}");
            }
        }

        public const string AutoMergeLabelText = "auto-merge";
        public const string MergeConflictsLabelText = "Merge Conflicts";
    }
}
