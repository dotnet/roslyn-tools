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

private static async Task RunAsync(ExecutionContext context)
{
    var gh = new GithubMergeTool.GithubMergeTool("dotnet-automerge-bot@users.noreply.github.com", await GetSecret("dotnet-automerge-bot-token"));
    var configPath = Path.Combine(context.FunctionDirectory, "config.xml");
    var config = XDocument.Load(configPath).Root;
    foreach (var repo in config.Elements("repo"))
    {
        var owner = repo.Attribute("owner").Value;
        var name = repo.Attribute("name").Value;
        var (autoMergeablePrs, error) = await gh.FetchAutoMergeablePrs(owner, name);
        if (error != null)
        {
            Log.Error($"Unable to fetch auto-mergeable PRs from '{owner}/{name}': {error.Content}");
            continue;
        }

        foreach (var pr in autoMergeablePrs)
        {
            var prIdentifier = $"{owner}/{name}:{pr}";
            Log.Info("Checking " + prIdentifier);
            var (merged, message, mergeError) = await gh.MergeAutoMergeablePr(owner, name, pr);
            if (merged)
            {
                Log.Info($"Auto-merged PR '{prIdentifier}'.");
            }
            else if (message != null)
            {
                Log.Info($"PR '{prIdentifier}' not a candidate for auto-merging: {message}");
            }
            else if (mergeError != null)
            {
                Log.Error($"Unable to auto-merge PR '{prIdentifier}': {await mergeError.Content.ReadAsStringAsync()}");
            }
            else
            {
                Log.Error($"Unable to auto-merg PR '{prIdentifier}' for unknown reason.");
            }
            
            // Delay in order to avoid triggering GitHub rate limiting
            await Task.Delay(5000);
        }
    }
}

public static void Run(TimerInfo myTimer, TraceWriter log, ExecutionContext context)
{
    Log = log;

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

    RunAsync(context).GetAwaiter().GetResult();
}
