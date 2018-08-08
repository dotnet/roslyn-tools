// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using NLog;

namespace VstsMergeTool
{
    class MergeToolEntry
    {
        static void Main(string[] args)
        {
            // Args[0] is used as Source Branch, Args[1] is use as Target Branch
            // TODO: Make the command line works as --Source 15.8x --Target 15.9x and possible argument checks

            Logger logger = LogManager.GetCurrentClassLogger();
            var initializer = new Initializer(args[0], args[1]);
            var result = initializer.MergeTool.CreatePullRequest().Result;
            logger.Info("Auto Merge Finished");
        }
    }
}
