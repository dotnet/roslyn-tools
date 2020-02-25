// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;

public class Program
{
    public static async Task Main(string[] args)
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
        await RunAsync(config, isAutomated, isDryRun, githubToken);
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
    /// <returns>true if we encounter a recoverable error, false if unrecoverable.</returns>
    private static async Task<bool> MakeGithubPr(
        GithubMergeTool.GithubMergeTool gh,
        string repoOwner,
        string repoName,
        string srcBranch,
        string destBranch,
        bool updateExistingPr,
        bool addAutoMergeLabel,
        bool isAutomatedRun,
        bool isDryRun,
        string githubToken)
    {
        Console.WriteLine($"Merging {repoName} from {srcBranch} to {destBranch}");

        var (prCreated, error) = await gh.CreateMergePr(repoOwner, repoName, srcBranch, destBranch, updateExistingPr, addAutoMergeLabel, isAutomatedRun);

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
            Console.WriteLine($"##vso[task.logissue type=error]Error creating PR. GH response code: {error.StatusCode}");
            Console.WriteLine($"##vso[task.logissue type=error]{await error.Content.ReadAsStringAsync()}");

            // Github rate limits are much lower for unauthenticated users.  We will definitely hit a rate limit
            // If we hit it during a dryrun, just bail out.
            if (isDryRun && string.IsNullOrWhiteSpace(githubToken))
            {
                if (TryGetRemainingRateLimit(error.Headers, out var remainingRateLimit) && remainingRateLimit == 0)
                {
                    Console.WriteLine($"##vso[task.logissue type=error]Hit GitHub rate limit in dryrun with no auth token.  Bailing out.");
                    return false;
                }
            }
        }

        return true;
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

    private static async Task RunAsync(XDocument config, bool isAutomatedRun, bool isDryRun, string githubToken)
    {
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

                if (!ShouldRunMerge(merge, isAutomatedRun))
                {
                    continue;
                }

                var addAutoMergeLabel = bool.Parse(merge.Attribute("addAutoMergeLabel")?.Value ?? "true");
                try
                {
                    bool shouldContinue = await MakeGithubPr(gh, owner, name, fromBranch, toBranch,
                        updateExistingPr, addAutoMergeLabel, isAutomatedRun, isDryRun, githubToken);
                    if (!shouldContinue)
                    {
                        return;
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
    }

    /// <summary>
    /// Checks the merge element for a frequency attribute. Then determines whether the current run
    /// matches the frequency criteria. Valid frequency values are 'daily' and 'weekly'.
    /// </summary>
    private static bool ShouldRunMerge(XElement merge, bool isAutomatedRun)
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

        // Since this is run on AzDO as an automated cron pipeline, times are in UTC.
        // See https://docs.microsoft.com/en-us/azure/devops/pipelines/build/triggers?view=azure-devops&tabs=yaml#scheduled-triggers
        var runDateTime = DateTime.UtcNow;
        var currentDate = runDateTime.Date;

        // Since cron should schedule this to run every 3 hours starting at 12am,
        // we can check if we are running within the first 15 minutes of the day.
        var fifteenMinutes = new TimeSpan(hours: 0, minutes: 15, seconds: 0);
        var isStartOfDay = runDateTime - currentDate < fifteenMinutes;

        // Daily and Weekly runs only happen at the start of the day
        if (!isStartOfDay)
        {
            return false;
        }

        var isSunday = currentDate.DayOfWeek != DayOfWeek.Sunday;

        // Weekly runs only happen on Sunday
        if (frequency == "weekly")
        {
            return isSunday;
        }

        // Daily runs happen every day
        return true;
    }
}
