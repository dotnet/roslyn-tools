// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Xml.Linq;

public class Program
{
    public static void Main(string[] args)
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
        RunAsync(config, isAutomated, isDryRun, githubToken).GetAwaiter().GetResult();
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

    private static async Task MakeGithubPr(
        GithubMergeTool.GithubMergeTool gh,
        string repoOwner,
        string repoName,
        string srcBranch,
        string destBranch,
        bool addAutoMergeLabel,
        bool isAutomatedRun)
    {
        Console.WriteLine($"Merging {repoName} from {srcBranch} to {destBranch}");

        var (prCreated, error) = await gh.CreateMergePr(repoOwner, repoName, srcBranch, destBranch, addAutoMergeLabel, isAutomatedRun);

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
        }
    }

    private static async Task RunAsync(XDocument config, bool isAutomatedRun, bool isDryRun, string githubAuthToken)
    {
        var gh = new GithubMergeTool.GithubMergeTool("dotnet-bot@users.noreply.github.com", githubAuthToken, isDryRun);
        foreach (var repo in config.Root.Elements("repo"))
        {
            var owner = repo.Attribute("owner").Value;
            var name = repo.Attribute("name").Value;
            foreach (var merge in repo.Elements("merge"))
            {
                var fromBranch = merge.Attribute("from").Value;
                var toBranch = merge.Attribute("to").Value;

                var frequency = merge.Attribute("frequency")?.Value;
                if (isAutomatedRun && frequency == "weekly" && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                {
                    continue;
                }

                var addAutoMergeLabel = bool.Parse(merge.Attribute("addAutoMergeLabel")?.Value ?? "true");
                try
                {
                    await MakeGithubPr(gh, owner, name, fromBranch, toBranch, addAutoMergeLabel, isAutomatedRun);
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
}
