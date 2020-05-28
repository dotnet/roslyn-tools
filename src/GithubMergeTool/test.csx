// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// README: This is a simple test script for trying out local changes.
// Nothing in this file runs in production.

#r "../../artifacts/bin/GithubMergeTool/Debug/netcoreapp2.1/GithubMergeTool.dll"

using System;
using System.Net;
using System.Threading.Tasks;

private readonly static string GithubUsername = Environment.GetEnvironmentVariable("GITHUB_USERNAME");
private readonly static string GithubAuthToken = Environment.GetEnvironmentVariable("GITHUB_AUTH_TOKEN");

private static async Task MakeGithubPr(
    GithubMergeTool.GithubMergeTool gh,
    string repoOwner,
    string repoName,
    string srcBranch,
    string destBranch)
{
    Console.WriteLine($"Merging from {srcBranch} to {destBranch}");

    var (prCreated, error) = await gh.CreateMergePr(repoOwner, repoName, new List<string>(), srcBranch, destBranch, updateExistingPr: true, addAutoMergeLabel: false, isAutoTriggered: false);

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
        Console.WriteLine($"Error creating PR. GH response code: {error.StatusCode}");
    }
}

private static async Task UpdateExistingPr(
    GithubMergeTool.GithubMergeTool gh,
    string repoOwner,
    string repoName,
    int? prNumber
)
{
    var (prs, error) = await gh.FetchOpenMergePRsAsync(repoOwner, repoName);

    if (!error.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error finding open merge PRs. GH response code: {error.StatusCode}");
        return;
    }
    else if (prs.Count == 0)
    {
        Console.WriteLine($"Did not find any open merge PRs.");
        return;
    }

    var mergePr = prNumber.HasValue
        ? prs.FirstOrDefault(pr => pr.Number == prNumber)
        : prs.First();

    if (mergePr is null)
    {
        Console.WriteLine($"Did not find an open merge PR with the number {prNumber}.");
        return;
    }

    Console.WriteLine($"Updating merge PR {prNumber}.");

    await MakeGithubPr(gh, repoOwner, repoName, mergePr.SrcBranch, mergePr.DestBranch);
}

private static async Task RunAsync()
{
    const string repoOwner = "dotnet";
    const string repoName = "roslyn";
    const bool isDryRun = true;
    int? prNumber = 44532;

    Console.WriteLine($"Looking up open merge PRs...");

    // Write your test code here to test changes to the merge tool DLL
    var gh = new GithubMergeTool.GithubMergeTool(GithubUsername, GithubAuthToken, isDryRun);

    await UpdateExistingPr(gh, repoOwner, repoName, prNumber);
}

RunAsync().GetAwaiter().GetResult();
