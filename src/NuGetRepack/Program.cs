// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Roslyn.Tools.NuGet.Repack
{
    internal sealed class Program
    {
        private enum Operation
        {
            None = 0,
            Release = 1,
            PreRelease = 2,
        }

        private const int ExitCodeSuccess = 0;
        private const int ExitCodeInvalidArgument = 1;
        private const int ExitCodeError = 2;
        private const int ExitCodeMultipleErrors = 3;

        private static int Main(string[] args)
        {
            var operation = Operation.None;
            var packages = new List<string>();
            string outDirectory = null;

            try
            {
                int i = 0;
                while (i < args.Length)
                {
                    var arg = args[i++];

                    string ReadValue() => (i < args.Length) ? args[i++] : throw new InvalidDataException($"Missing value for option {arg}");

                    switch (arg)
                    {
                        case "/rel":
                        case "/release":
                            operation = Operation.Release;
                            break;

                        case "/prerel":
                        case "/prerelease":
                            operation = Operation.PreRelease;
                            break;

                        case "/out":
                            outDirectory = ReadValue();
                            break;

                        default:
                            if (arg.StartsWith("/", StringComparison.Ordinal))
                            {
                                throw new InvalidDataException($"Unrecognized option: '{arg}'");
                            }

                            if (Directory.Exists(arg))
                            {
                                foreach (var file in Directory.GetFiles(arg))
                                {
                                    if (file.EndsWith(".nupkg"))
                                    {
                                        packages.Add(file);
                                    }
                                }
                            }
                            else
                            {
                                packages.Add(arg);
                            }

                            break;
                    }
                }

                switch (operation)
                {
                    case Operation.Release:
                    case Operation.PreRelease:
                        if (packages.Count == 0)
                        {
                            throw new InvalidDataException($"Must specify at least one package");
                        }

                        break;

                    default:
                        throw new InvalidDataException($"Operation not specified");
                }
            }
            catch (InvalidDataException e)
            {
                Console.Error.WriteLine("Usage: NuGetRepack <operation> <options> <packages>");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Operation:");
                Console.Error.WriteLine("  /rel[ease]           Strip pre-release version suffix from versions of specified package(s).");
                Console.Error.WriteLine("  /prerel[ease]        Strip per-build version suffix from versions of specified package(s).");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Options:");
                Console.Error.WriteLine("  /out <path>          Optional path to an output directory. Validation is performed if not specified.");
                Console.Error.WriteLine();
                Console.Error.WriteLine("<packages>             Paths to .nupkg files.");
                Console.Error.WriteLine(e.Message);
                return ExitCodeInvalidArgument;
            }

            try
            {
                switch (operation)
                {
                    case Operation.Release:
                        VersionUpdater.Run(packages, outDirectory, release: true);
                        break;

                    case Operation.PreRelease:
                        VersionUpdater.Run(packages, outDirectory, release: false);
                        break;

                    default:
                        throw new InvalidDataException($"Operation not specified");
                }
            }
            catch (AggregateException e)
            {
                foreach (var inner in e.InnerExceptions)
                {
                    Console.Error.WriteLine(inner.Message);
                }

                return ExitCodeMultipleErrors;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return ExitCodeError;
            }

            return ExitCodeSuccess;
        }
    }
}
