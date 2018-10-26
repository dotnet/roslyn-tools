// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Mono.Options;

namespace BuildRetainer
{
    class Program
    {
        static int Main(string[] args)
        {
            var exeName = Assembly.GetExecutingAssembly().GetName().Name;
            var options = new Options();
            var showHelp = false;
            var parameters = new OptionSet()
            {
                $"Usage: {exeName} [options]",
                "Ensure shipped builds are retained and un-shipped builds are marked to be cleaned up.",
                "",
                "Options:",
                { "BuildQueueName=", "The name of the build queue.", value => options.BuildQueueName = value },
                { "ComponentName=", "The name of the inserted component.", value => options.ComponentName = value },
                { "ClientId=", "The ID used to get Azure auth tokens.", value => options.ClientId = value },
                { "ClientSecret=", "The secret used to get Azure auth tokens.", value => options.ClientId = value },
                { "h|?|help", "Show this message and exit.", value => showHelp = value != null }
            };

            try
            {
                parameters.Parse(args);
                if (showHelp || !options.IsValid)
                {
                    parameters.WriteOptionDescriptions(Console.Out);
                    return options.IsValid ? 0 : 1;
                }

                var br = new BuildRetainer(options);
                br.Run();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{exeName}: {e}");
                Console.WriteLine($"Try `{exeName} --help` for more information.");
                return 1;
            }
        }
    }
}
