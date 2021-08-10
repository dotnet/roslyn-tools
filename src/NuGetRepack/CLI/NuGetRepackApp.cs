// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Roslyn.Tools
{
    internal sealed class NuGetRepackApp
    {
        private const int ExitCodeSuccess = 0;
        private const int ExitCodeInvalidArgument = 1;
        private const int ExitCodeError = 2;
        private const int ExitCodeMultipleErrors = 3;

        private static int Main(string[] args)
        {
            var translation = VersionTranslation.None;
            var packages = new List<string>();
            string outDirectory = null;
            bool exactVersions = false;

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
                            translation = VersionTranslation.Release;
                            break;

                        case "/prerel":
                        case "/prerelease":
                            translation = VersionTranslation.PreRelease;
                            break;

                        case "/out":
                            outDirectory = ReadValue();
                            break;

                        case "/exactVersions":
                            exactVersions = true;
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

                if (packages.Count == 0)
                {
                    throw new InvalidDataException($"Must specify at least one package");
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
                Console.Error.WriteLine("  /exactVersions       Replace references among given packages with exact versions.");
                Console.Error.WriteLine("                       Use when packages tightly depend on each other (e.g. the binaries have InternalsVisibleTo).");
                Console.Error.WriteLine();
                Console.Error.WriteLine("<packages>             Paths to .nupkg files.");
                Console.Error.WriteLine(e.Message);
                return ExitCodeInvalidArgument;
            }

            try
            {
                NuGetVersionUpdater.Run(packages, outDirectory, translation, exactVersions);
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
