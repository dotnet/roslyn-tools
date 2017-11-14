// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private class OutdatedPackageException : Exception
        {
            public PackageInfo Package { get; private set; }
            public SemanticVersion PreviousPackage { get; private set; }

            public OutdatedPackageException(string message, PackageInfo package, SemanticVersion previousPackage)
                : base(message)
            {
                Package = package;
                PreviousPackage = previousPackage;
            }
        }

        /// <summary>
        /// Updates the specified NuGet packages.  Returns `true` if the package was successfully updated.
        /// </summary>
        private static bool UpdatePackages(
            List<string> newPackageFiles,
            BuildVersion roslynBuildVersion,
            CoreXT coreXT,
            string packagesDir,
            CancellationToken cancellationToken)
        {
            bool shouldRetainBuild = false;

            // All CoreXT packages we insert:
            var packagePaths = Directory.EnumerateFiles(packagesDir, "*.nupkg", SearchOption.AllDirectories);

            foreach (var packagePath in packagePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(packagePath);

                Log.Info($"Processing package '{packagePath}'");

                var package = PackageInfo.ParsePackageFileName(fileName);

                if (package.IsRoslynToolsetCompiler)
                {
                    // The toolset compiler is inserted separately
                    continue;
                }

                if (!coreXT.TryGetPackageVersion(package, out var previousPackageVersion))
                {
                    Log.Info($"New package is being inserted: '{package}'");

                    coreXT.AddNewPackage(package);
                    newPackageFiles.Add(fileName);
                    shouldRetainBuild = true;
                    continue;
                }

                UpdatePackage(previousPackageVersion, roslynBuildVersion, coreXT, package);
            }

            return shouldRetainBuild;
        }

        private static void UpdatePackage(
            SemanticVersion previousPackageVersion,
            BuildVersion roslynBuildVersion,
            CoreXT coreXT,
            PackageInfo package)
        {
            if (package.Version.Version < previousPackageVersion.Version)
            {
                var message = $"The version of package '{package}' is older than previously inserted '{previousPackageVersion}'.";

                if (package.IsRoslyn)
                {
                    throw new OutdatedPackageException(message, package, previousPackageVersion);
                }

                WarningMessages.Add(message);
            }

            if (package.IsRoslyn)
            {
                var buildVersion = package.Version.GetSuffixBuildVersion();

                if (buildVersion.Build != roslynBuildVersion.FiveDigitBuildNumber ||
                    buildVersion.Revision != roslynBuildVersion.Revision)
                {
                    throw new InvalidOperationException($"Roslyn package version '{package.Version}' inconsistent with build version '{roslynBuildVersion}'");
                }
            }

            if (package.Version == previousPackageVersion)
            {
                Log.Info($"Package '{package}' doesn't need to be inserted, version matches the one already inserted.");
            }
            else
            {
                Log.Info($"Package '{package}' needs to be inserted, previously inserted version is {previousPackageVersion}");

                // update .corext\Configs\default.config:
                coreXT.UpdatePackageVersion(package);
            }
        }
    }
}
