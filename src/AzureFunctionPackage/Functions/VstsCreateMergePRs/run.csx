// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "System.Xml.Linq"
#r ".\VstsMergeTool.dll"

#load "auth.csx"

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

private static TraceWriter Log = null;

private static async Task MakeVstsPr(string repoName, string srcBranch, string destBranch)
{
    Log.Info($"Merging {repoName} from {srcBranch} to {destBranch}");

    VstsMergeTool.Initializer initializer = new VstsMergeTool.Initializer(srcBranch, destBranch);

    var response = await initializer.MergeTool.CreatePullRequest(); ;

    if (response)
    {
        Log.Info("PR created successfully");
    }
    else
    {
        Log.Info("PR creation skipped. PR already exists or all commits are present in base branch");
    }
}

private static async Task RunAsync(ExecutionContext context)
{
    var configPath = Path.Combine(context.FunctionDirectory, "VstsPrConfig.xml");
    var config = XDocument.Load(configPath).Root;
    foreach (var repo in config.Elements("repo"))
    {
        var repoName = repo.Attribute("name").Value;
        foreach (var merge in repo.Elements("merge"))
        {
            var fromBranch = merge.Attribute("from").Value;
            var toBranch = merge.Attribute("to").Value;
            await MakeVstsPr(repoName, fromBranch, toBranch);
        }
    }
}

public static void Run(TimerInfo myTimer, TraceWriter log, ExecutionContext context)
{
    Log = log;

    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

    RunAsync(context).GetAwaiter().GetResult();
}
