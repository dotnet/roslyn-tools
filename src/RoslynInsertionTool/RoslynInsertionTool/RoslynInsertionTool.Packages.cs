// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
            BuildVersion buildVersion,
            CoreXT coreXT,
            string packagesDir,
            ImmutableArray<string> packagesToBeIgnored,
            bool skipPackageVersionValidation,
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

                if (package.IsRoslynToolsetCompiler || packagesToBeIgnored.Any(p => p == package.PackageName))
                {
                    continue;
                }

                if (!coreXT.TryGetPackageVersion(package, out var previousPackageVersion))
                {
                    LogWarning($"Could not find existing version for package: '{package}'. Add a reference to it manually.");
                    continue;
                }

                UpdatePackage(previousPackageVersion, coreXT, package, skipPackageVersionValidation);
            }

            return (shouldRetainBuild, newPackageFiles);
        }

        private static void UpdatePackage(
            NuGetVersion previousPackageVersion,
            CoreXT coreXT,
            PackageInfo package,
            bool skipPackageVersionValidation)
        {
            if (!skipPackageVersionValidation && package.Version < previousPackageVersion)
            {
                if (package.IsRoslyn)
                {
                    throw new OutdatedPackageException(
                        $"The version of package '{package}' is older than previously inserted '{previousPackageVersion}'.",
                        package,
                        previousPackageVersion);
                }

                Console.WriteLine($"Package '{package}' doesn't need to be inserted, version is lower than the one already inserted.");
                return;
            }

            if (package.Version != previousPackageVersion)
            {
                Console.WriteLine($"Package '{package}' needs to be inserted, previously inserted version is {previousPackageVersion}");

                // update .corext\Configs\default.config and any other props files under src\ConfigData\Packages
                coreXT.UpdatePackageVersion(package);
                return;
            }
            else
            {
                Console.WriteLine($"Package '{package}' has the same version.");
            }
            
        }
    }
}
