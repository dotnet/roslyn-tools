// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// README: This is a simple test script for trying out local changes.
// Nothing in this file runs in production.

#r "../../artifacts/bin/GithubMergeTool/Debug/net46/GithubMergeTool.dll"

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

    var (prCreated, error) = await gh.CreateMergePr(repoOwner, repoName, srcBranch, destBranch, addAutoMergeLabel: false, isAutoTriggered: false);

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

private static async Task RunAsync()
{
    // Write your test code here to test changes to the merge tool DLL
    var gh = new GithubMergeTool.GithubMergeTool(GithubUsername, GithubAuthToken);

    var (prs, error) = await gh.FetchAutoMergeablePrs("dotnet", "roslyn");
    foreach (var pr in prs)
    {
        Console.WriteLine(pr);
    }
}

RunAsync().GetAwaiter().GetResult();
