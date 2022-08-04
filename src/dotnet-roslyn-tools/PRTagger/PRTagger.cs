// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRTagger;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Extensions;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

internal class PRTagger
{
    /// <summary>
    /// Creates a GitHub issue containing the PRs inserted into a given VS build.
    /// </summary>
    /// <param name="gitHubRepoUrl">GitHub repo URL</param>
    /// <param name="componentJsonFileName">Name of component JSON file (e.g. '.corext\Configs\dotnetcodeanalysis-components.json')</param>
    /// <param name="componentName">Name of component within JSON file (e.g. 'Microsoft.CodeAnalysis.LanguageService')</param>
    /// <param name="buildPipelineName">Name of build pipeline (e.g. 'dotnet-roslyn CI')</param>
    /// <param name="vsBuild">VS build number</param>
    /// <param name="vsCommitSha">Commit SHA for VS build</param>
    /// <param name="settings">Authentication tokens</param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Exit code indicating whether issue was successfully created.</returns>
    public static async Task<int> TagPRs(
        string gitHubRepoUrl,
        string componentJsonFileName,
        string componentName,
        string buildPipelineName,
        string vsBuild,
        string vsCommitSha,
        RoslynToolsSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var devdivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);
        using var dncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);

        // Retrieve VS repo
        var vsRepository = await GetVSRepositoryAsync(devdivConnection.GitClient);

        // Find previous VS commit SHA
        var vsCommit = await devdivConnection.GitClient.GetCommitAsync(
            vsCommitSha, vsRepository.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        var previousVsCommitSha = vsCommit.Parents.First();

        // Get associated product build for current and previous VS commit SHAs
        var currentBuild = await TryGetBuildNumberForReleaseAsync(componentJsonFileName, componentName, vsCommitSha, devdivConnection).ConfigureAwait(false);
        var previousBuild = await TryGetBuildNumberForReleaseAsync(componentJsonFileName, componentName, previousVsCommitSha, devdivConnection).ConfigureAwait(false);

        var gitHubRepoName = gitHubRepoUrl.Split('/').Last();

        if (currentBuild is null)
        {
            logger.LogError($"{gitHubRepoName} build not found for VS commit SHA {currentBuild}.");
            return -1;
        }

        if (previousBuild is null)
        {
            logger.LogError($"{gitHubRepoName} build not found for VS commit SHA {previousBuild}.");
            return -1;
        }

        // If builds are the same, there are no PRs to tag
        if (currentBuild.Equals(previousBuild))
        {
            logger.LogInformation($"No PRs found to tag; {gitHubRepoName} build numbers are equal: {currentBuild}.");
            return 0;
        }

        // Get commit SHAs for product builds
        var previousProductCommitSha = await TryGetProductCommitShaFromBuildAsync(buildPipelineName, dncengConnection, previousBuild).ConfigureAwait(false);
        var currentProductCommitSha = await TryGetProductCommitShaFromBuildAsync(buildPipelineName, dncengConnection, currentBuild).ConfigureAwait(false);

        if (previousProductCommitSha is null || currentProductCommitSha is null)
        {
            logger.LogError($"Error retrieving {gitHubRepoName} commit SHAs.");
            return -1;
        }

        logger.LogInformation($"Finding PRs between {gitHubRepoName} commit SHAs {previousProductCommitSha} and {currentProductCommitSha}.");

        // Retrieve GitHub repo
        string? gitHubRepoPath = null;
        try
        {
            gitHubRepoPath = Environment.CurrentDirectory + "\\" + gitHubRepoName;
            if (!Repository.IsValid(gitHubRepoPath))
            {
                logger.LogInformation("Cloning GitHub repo...");
                gitHubRepoPath = Repository.Clone(gitHubRepoUrl, workdirPath: gitHubRepoPath);
            }
            else
            {
                logger.LogInformation($"Repo already exists at {gitHubRepoPath}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception while cloning repo: " + ex);
            return -1;
        }

        // Find PRs between product commit SHAs
        var prDescription = new StringBuilder();
        var isSuccess = PRFinder.PRFinder.FindPRs(previousProductCommitSha, currentProductCommitSha, logger, gitHubRepoPath, prDescription);
        if (isSuccess != 0)
        {
            return isSuccess;
        }

        logger.LogInformation($"Creating issue...");

        // Create issue
        var issueCreated = await TryCreateIssue(gitHubRepoName, vsBuild, prDescription.ToString(), settings.GitHubToken, logger);
        return issueCreated ? 0 : -1;
    }

    private static async Task<GitRepository> GetVSRepositoryAsync(GitHttpClient gitClient)
    {
        return await gitClient.GetRepositoryAsync("DevDiv", "VS");
    }

    private static async Task<string?> TryGetBuildNumberForReleaseAsync(
        string componentJsonFileName,
        string componentName,
        string vsCommitSha,
        AzDOConnection vsConnection)
    {
        var url = await VisualStudioRepository.GetUrlFromComponentJsonFileAsync(
            vsCommitSha, GitVersionType.Commit, vsConnection, componentJsonFileName, componentName);
        if (url is null)
        {
            return null;
        }

        try
        {
            var buildNumber = VisualStudioRepository.GetBuildNumberFromUrl(url);
            return buildNumber;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> TryGetProductCommitShaFromBuildAsync(
        string buildPipelineName,
        AzDOConnection buildConnection,
        string buildNumber)
    {
        var build = (await buildConnection.TryGetBuildsAsync(buildPipelineName, buildNumber))?.SingleOrDefault();
        return build?.SourceVersion;
    }

    private async static Task<bool> TryCreateIssue(
        string gitHubRepoName,
        string vsBuildNumber,
        string issueBody,
        string gitHubToken,
        ILogger logger)
    {
        var client = new HttpClient
        {
            BaseAddress = new("https://api.github.com/")
        };

        var authArray = Encoding.ASCII.GetBytes($"{gitHubToken}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(authArray));
        client.DefaultRequestHeaders.Add(
            "user-agent",
            "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");

        // https://docs.github.com/en/rest/issues/issues#create-an-issue
        var response = await client.PostAsyncAsJson($"repos/dotnet/{gitHubRepoName}/issues", JsonConvert.SerializeObject(
            new
            {
                title = $"[Automated] PRs inserted in VS build {vsBuildNumber}",
                body = issueBody,
                labels = new string[] { "vs-insertion" }
            }));

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Issue creation failed with status code: {response.StatusCode}");
            return false;
        }

        logger.LogInformation("Successfully created issue.");
        return true;
    }
}
