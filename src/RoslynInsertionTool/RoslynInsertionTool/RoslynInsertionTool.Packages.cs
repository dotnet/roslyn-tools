// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NuGet.Versioning;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private class OutdatedPackageException : Exception
        {
            public PackageInfo Package { get; private set; }
            public NuGetVersion PreviousPackage { get; private set; }

            public OutdatedPackageException(string message, PackageInfo package, NuGetVersion previousPackage)
                : base(message)
            {
                Package = package;
                PreviousPackage = previousPackage;
            }
        }

        /// <summary>
        /// Updates the specified NuGet packages.  Returns `true` if the package was successfully updated.
        /// </summary>
        private static (bool success, List<string> newPackageFiles) UpdatePackages(
            CoreXT coreXT,
            string packagesDir,
            CancellationToken cancellationToken)
        {
            bool shouldRetainBuild = false;
            var newPackageFiles = new List<string>();

            // All CoreXT packages we insert:
            var packagePaths = Directory.EnumerateFiles(packagesDir, "*.nupkg", SearchOption.AllDirectories);

            foreach (var packagePath in packagePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(packagePath);

                Console.WriteLine($"Processing package '{packagePath}'");

                var package = PackageInfo.ParsePackageFileName(fileName);

                if (package.IsRoslynToolsetCompiler)
                {
                    // The toolset compiler is inserted separately
                    continue;
                }

                if (!coreXT.TryGetPackageVersion(package, out var previousPackageVersion))
                {
                    Console.WriteLine($"New package is being inserted: '{package}'");

                    coreXT.AddNewPackage(package);
                    newPackageFiles.Add(fileName);
                    shouldRetainBuild = true;
                    continue;
                }

                UpdatePackage(previousPackageVersion, coreXT, package);
            }

            return (shouldRetainBuild, newPackageFiles);
        }

        private static void UpdatePackage(
            NuGetVersion previousPackageVersion,
            CoreXT coreXT,
            PackageInfo package)
        {
            if (package.IsRoslyn)
            {
                if (package.Version < previousPackageVersion)
                {
                    throw new OutdatedPackageException(
                        $"The version of package '{package}' is older than previously inserted '{previousPackageVersion}'.",
                        package,
                        previousPackageVersion);
                }
            }

            if (package.Version <= previousPackageVersion)
            {
                Console.WriteLine($"Package '{package}' doesn't need to be inserted, version is lower than or equal to the one already inserted.");
            }
            else
            {
                Console.WriteLine($"Package '{package}' needs to be inserted, previously inserted version is {previousPackageVersion}");

                // update .corext\Configs\default.config:
                coreXT.UpdatePackageVersion(package);
            }
        }
    }
}
