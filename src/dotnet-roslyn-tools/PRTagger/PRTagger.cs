// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRTagger;

using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Extensions;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

internal class PRTagger
{
    private static readonly Roslyn s_roslynInfo = new();

    // TO-DO: Catch potential exceptions
    public static async Task<int> TagPRs(
        string vsBuild,
        string vsCommitSha,
        RoslynToolsSettings settings,
        string gitHubUsername,
        string gitHubPassword,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Set up connection to DevDiv AzDo and get VS repo
        using var devdivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);
        var vsRepository = await GetVSRepositoryAsync(devdivConnection.GitClient);

        // Find previous VS commit SHA
        var vsCommit = await devdivConnection.GitClient.GetCommitAsync(
            vsCommitSha, vsRepository.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        var previousVsCommitSha = vsCommit.Parents.First();

        // Get associated Roslyn build for current and previous VS commit SHAs
        var currentRoslynBuild = await TryGetRoslynBuildNumberForReleaseAsync(vsCommitSha, devdivConnection).ConfigureAwait(false);
        var previousRoslynBuild = await TryGetRoslynBuildNumberForReleaseAsync(previousVsCommitSha, devdivConnection).ConfigureAwait(false);

        if (currentRoslynBuild is null)
        {
            logger.LogError($"Roslyn build not found for VS commit SHA {currentRoslynBuild}");
            return -1;
        }

        if (previousRoslynBuild is null)
        {
            logger.LogError($"Roslyn build not found for VS commit SHA {previousRoslynBuild}");
            return -1;
        }

        // If builds are the same, there are no PRs to tag
        if (currentRoslynBuild.Equals(previousRoslynBuild))
        {
            logger.LogInformation($"No PRs found to tag; Roslyn build numbers are equal: {currentRoslynBuild}");
            return 0;
        }

        using var dncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);
        var previousRoslynCommitSha = await TryGetRoslynCommitShaFromBuildAsync(dncengConnection, previousRoslynBuild, logger).ConfigureAwait(false);
        var currentRoslynCommitSha = await TryGetRoslynCommitShaFromBuildAsync(dncengConnection, currentRoslynBuild, logger).ConfigureAwait(false);

        if (previousRoslynCommitSha is null || currentRoslynCommitSha is null)
        {
            logger.LogError($"Error retrieving Roslyn commit SHAs.");
            return -1;
        }

        var prDescription = new StringBuilder();
        var isSuccess = PRFinder.PRFinder.FindPRs(previousRoslynCommitSha, currentRoslynCommitSha, logger, prDescription);
        if (isSuccess != 0)
        {
            return -1;
        }
        var issueCreated = await TryCreateIssue(vsBuild, prDescription.ToString(), gitHubUsername, gitHubPassword, logger);
        return issueCreated ? 0 : 1;
    }

    private static async Task<GitRepository> GetVSRepositoryAsync(GitHttpClient gitClient)
    {
        return await gitClient.GetRepositoryAsync("DevDiv", "VS");
    }

    private static async Task<string?> TryGetRoslynBuildNumberForReleaseAsync(
        string vsCommitSha,
        AzDOConnection vsConnection)
    {
        var url = await VisualStudioRepository.GetUrlFromComponentJsonFileAsync(
            vsCommitSha, GitVersionType.Commit, vsConnection, s_roslynInfo.ComponentJsonFileName, s_roslynInfo.ComponentName);
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

    private static async Task<string?> TryGetRoslynCommitShaFromBuildAsync(
        AzDOConnection buildConnection,
        string buildNumber)
    {
        var build = (await buildConnection.TryGetBuildsAsync(
            s_roslynInfo.GetBuildPipelineName(buildConnection.BuildProjectName)!, buildNumber))?.SingleOrDefault();

        if (build == null)
        {
            return null;
        }

        return build.SourceVersion;
    }

    private async static Task<bool> TryCreateIssue(
        string vsBuildNumber,
        string issueBody,
        string gitHubUsername,
        string gitHubPassword,
        ILogger logger)
    {
        var client = new HttpClient
        {
            BaseAddress = new("https://api.github.com/")
        };

        var authArray = Encoding.ASCII.GetBytes($"{gitHubUsername}:{gitHubPassword}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(authArray));
        client.DefaultRequestHeaders.Add(
            "user-agent",
            "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");

        // Needed to call the check-runs endpoint
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.antiope-preview+json"));

        // TO-DO: Generalize for other repos
        // https://developer.github.com/v3/pulls/#create-a-pull-request
        var response = await client.PostAsyncAsJson($"repos/dotnet/roslyn/issues", JsonConvert.SerializeObject(
            new
            {
                title = $"[Automated] PRs in VS build {vsBuildNumber}",
                body = issueBody,
            }));

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully created issue.");
            return true;
        }
        else
        {
            logger.LogInformation($"Issue creation failed with status code: {response.StatusCode}");
            return false;
        }
    }
}
