// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        /// (false, null) if the PR wasn't created due to a PR already existing
        /// or if the <paramref name="destBranch"/> contains all the commits
        /// from <paramref name="srcBranch"/>.
        /// (false, error response) if there was an error creating the PR.
        /// </returns>
        public async Task<(bool prCreated, HttpResponseMessage error)> CreateMergePr(
            string repoOwner,
            string repoName,
            string srcBranch,
            string destBranch)
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

            jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());

            // 422 (Unprocessable Entity) indicates there were no commits to merge
            if (response.StatusCode == (HttpStatusCode)422)
            {
                // Delete the pr branch if the PR was not created.
                await _client.DeleteAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{prBranchName}");
                return (false, null);
            }

            return (true, null);
        }
    }
}
