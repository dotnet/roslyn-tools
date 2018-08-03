// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "System.Xml.Linq"
#r ".\GithubMergeTool.dll"

#load "auth.csx"

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

private static TraceWriter Log = null;

private static async Task MakeGithubPr(
    GithubMergeTool.GithubMergeTool gh,
    string repoOwner,
    string repoName,
    string srcBranch,
    string destBranch,
    bool addAutoMergeLabel = false)
{
    Log.Info($"Merging {repoName} from {srcBranch} to {destBranch}");

    var (prCreated, error) = await gh.CreateMergePr(repoOwner, repoName, srcBranch, destBranch, addAutoMergeLabel);

    if (prCreated)
    {
        Log.Info("PR created successfully");
    }
    else if (error == null)
    {
        Log.Info("PR creation skipped. PR already exists or all commits are present in base branch");
    }
    else
    {
        Log.Error($"Error creating PR. GH response code: {error.StatusCode}");
        Log.Error(await error.Content.ReadAsStringAsync());
    }
}

private static async Task RunAsync(ExecutionContext context)
{
    var gh = new GithubMergeTool.GithubMergeTool("dotnet-bot@users.noreply.github.com", await GetSecret("dotnet-bot-github-auth-token"));
    var configPath = Path.Combine(context.FunctionDirectory, "config.xml");
    var config = XDocument.Load(configPath).Root;
    foreach (var repo in config.Elements("repo"))
    {
        var owner = repo.Attribute("owner").Value;
        var name = repo.Attribute("name").Value;
        foreach (var merge in repo.Elements("merge"))
        {
            var fromBranch = merge.Attribute("from").Value;
            var toBranch = merge.Attribute("to").Value;
            var addAutoMergeLabel = bool.Parse(merge.Attribute("addAutoMergeLabel")?.Value ?? "true");
            await MakeGithubPr(gh, owner, name, fromBranch, toBranch, addAutoMergeLabel);
        }
    }
}

public static void Run(TimerInfo myTimer, TraceWriter log, ExecutionContext context)
{
    Log = log;

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

    RunAsync(context).GetAwaiter().GetResult();
}
