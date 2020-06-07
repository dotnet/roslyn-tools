// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Default when no args passed in.
        var isDryRun = true;
        var isAutomated = false;
        var githubToken = string.Empty;

        foreach (var arg in args)
        {
            if (arg.StartsWith("--isDryRun"))
            {
                isDryRun = bool.Parse(GetArgumentValue(arg));
            }
            else if (arg.StartsWith("--isAutomated"))
            {
                isAutomated = bool.Parse(GetArgumentValue(arg));
            }
            else if (arg.StartsWith("--githubToken"))
            {
                githubToken = GetArgumentValue(arg);
            }
        }

        Console.WriteLine($"Executing with {nameof(isDryRun)}={isDryRun}, {nameof(isAutomated)}={isAutomated}, {nameof(githubToken)}={githubToken}");
        var config = GetConfig();
        var success = await RunAsync(config, isAutomated, isDryRun, githubToken);
        return success ? 0 : 1;
    }

    private static string GetArgumentValue(string arg)
    {
        var split = arg.Split(new char[] { ':', '=' }, 2);
        return split[1];
    }

    private static XDocument GetConfig()
    {
        var assembly = typeof(Program).Assembly;
        using (var stream = assembly.GetManifestResourceStream("GitHubCreateMergePRs.config.xml"))
        {
            return XDocument.Load(stream);
        }
    }

    /// <summary>
    /// Make requests to github to merge a source branch into a destination branch.
    /// </summary>
    private static async Task<(bool success, bool shouldContinue)> MakeGithubPr(
        GithubMergeTool.GithubMergeTool gh,
        string repoOwner,
        string repoName,
        List<string> prOwners,
        string srcBranch,
        string destBranch,
        bool updateExistingPr,
        bool addAutoMergeLabel,
        bool isAutomatedRun,
        bool isDryRun,
        string githubToken)
    {
        Console.WriteLine($"Merging {repoName} from {srcBranch} to {destBranch}");

        var (prCreated, error) = await gh.CreateMergePr(repoOwner, repoName, prOwners, srcBranch, destBranch, updateExistingPr, addAutoMergeLabel, isAutomatedRun);

        if (prCreated)
        {
            Console.WriteLine("PR created successfully");
        }
        else if (error == null)
        {
            Console.WriteLine("PR creation skipped. PR already exists or all commits are present in base branch");
        }
        else
        {
            var errorStatus = error.StatusCode;
            var isWarning = errorStatus == HttpStatusCode.UnprocessableEntity || errorStatus == HttpStatusCode.NotFound;
            var issueType = isWarning ? "warning" : "error";
            Console.WriteLine($"##vso[task.logissue type={issueType}]Error creating PR. GH response code: {error.StatusCode}");
            Console.WriteLine($"##vso[task.logissue type={issueType}]{await error.Content.ReadAsStringAsync()}");

            // Github rate limits are much lower for unauthenticated users.  We will definitely hit a rate limit
            // If we hit it during a dryrun, just bail out.
            if (isDryRun && string.IsNullOrWhiteSpace(githubToken))
            {
                if (TryGetRemainingRateLimit(error.Headers, out var remainingRateLimit) && remainingRateLimit == 0)
                {
                    Console.WriteLine($"##vso[task.logissue type=error]Hit GitHub rate limit in dryrun with no auth token.  Bailing out.");
                    return (success: false, shouldContinue: false);
                }
            }


            return (success: isWarning, shouldContinue: true);
        }

        return (success: true, shouldContinue: true);
    }

    private static bool TryGetRemainingRateLimit(HttpResponseHeaders headers, out int remainingRateLimit)
    {
        if (headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
        {
            if (int.TryParse(remainingValues.First(), out remainingRateLimit))
            {
                return true;
            }
        }

        remainingRateLimit = -1;
        return false;
    }

    // Returns 'true' if all PRs were created/updated successfully.
    private static async Task<bool> RunAsync(XDocument config, bool isAutomatedRun, bool isDryRun, string githubToken)
    {
        // Since this is run on AzDO as an automated cron pipeline, times are in UTC.
        // See https://docs.microsoft.com/en-us/azure/devops/pipelines/build/triggers?view=azure-devops&tabs=yaml#scheduled-triggers
        var runDateTime = DateTime.UtcNow;

        var allSuccess = true;
        var gh = new GithubMergeTool.GithubMergeTool("dotnet-bot@users.noreply.github.com", githubToken, isDryRun);
        foreach (var repo in config.Root.Elements("repo"))
        {
            var owner = repo.Attribute("owner").Value;
            var name = repo.Attribute("name").Value;

            // We don't try to update existing PR unless asked.
            var updateExistingPr = bool.Parse(repo.Attribute("updateExistingPr")?.Value ?? "false");

            foreach (var merge in repo.Elements("merge"))
            {
                var fromBranch = merge.Attribute("from").Value;
                var toBranch = merge.Attribute("to").Value;

                if (!ShouldRunMerge(merge, isAutomatedRun, runDateTime))
                {
                    continue;
                }

                var prOwners = merge.Attribute("owners")?.Value.Split(',').ToList() ?? new List<string>();
                var addAutoMergeLabel = bool.Parse(merge.Attribute("addAutoMergeLabel")?.Value ?? "true");
                try
                {
                    var (success, shouldContinue) = await MakeGithubPr(gh, owner, name, prOwners, fromBranch, toBranch,
                        updateExistingPr, addAutoMergeLabel, isAutomatedRun, isDryRun, githubToken);
                    allSuccess = allSuccess && success;
                    if (!shouldContinue)
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("##vso[task.logissue type=error]Error creating merge PR.", ex);
                }
                finally
                {
                    // Delay in order to avoid triggering GitHub rate limiting
                    await Task.Delay(4000);
                }
            }
        }

        return allSuccess;
    }

    /// <summary>
    /// Checks the merge element for a frequency attribute. Then determines whether the current run
    /// matches the frequency criteria. Valid frequency values are 'daily' and 'weekly'.
    /// </summary>
    private static bool ShouldRunMerge(XElement merge, bool isAutomatedRun, DateTime runDateTime)
    {
        // We always run when merges are started manually
        if (!isAutomatedRun)
        {
            return true;
        }

        var frequency = merge.Attribute("frequency")?.Value.ToLower();

        // We always run when a frequency isn't specified
        if (string.IsNullOrEmpty(frequency))
        {
            return true;
        }

        // Throw when an unexpected frequency value is specified
        if (frequency != "daily" && frequency != "weekly")
        {
            throw new Exception($"Unexpected merge frequency specified: '{frequency}'. Valid values are 'daily' and 'weekly'.");
        }

        // Since cron should schedule this to run every 3 hours starting at 12am,
        // we expect to be run within a 10 minute window of this time.

        // Adjust the time 5 minutes into the future in case the pipeline machine
        // and scheduler machine have mismatched clocks.
        var adjustedRunDateTime = runDateTime.AddMinutes(5);
        var adjustedRunDate = adjustedRunDateTime.Date;

        // Because the adjusted run time is being used, we treat a run as valid if
        // it begins within the last 5 minutes of the previous day through the first
        // 5 minutes of the current day.
        var tenMinutes = new TimeSpan(hours: 0, minutes: 10, seconds: 0);
        var isStartOfDay = adjustedRunDateTime - adjustedRunDate < tenMinutes;

        // Daily and Weekly runs only happen at the start of the day
        if (!isStartOfDay)
        {
            return false;
        }

        // Weekly runs only happen on Sunday
        if (frequency == "weekly")
        {
            return adjustedRunDate.DayOfWeek == DayOfWeek.Sunday;
        }

        // Daily runs happen every day
        return true;
    }
}
