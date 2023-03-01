// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Diagnostics.Contracts;
using static Microsoft.RoslynTools.NuGet.Helpers;

namespace Microsoft.RoslynTools.NuGet
{
    internal class NuGetPrepare
    {
        private const string NotPublishedDirectoryName = "NotPublished";

        internal static async Task<int> PrepareAsync(ILogger logger)
        {
            try
            {
                string? version;
                var determinedVersion = TryDetermineRoslynPackageVersion(out version);

                if (!determinedVersion)
                {
                    logger.LogError("Expected packages are missing. Unable to determine version.");
                    return 1;
                }

                Contract.Assert(version is not null);

                logger.LogInformation($"Moving {version} packages...");

                var publishedPackages = RoslynPackageIds
                    .Select(packageId => GetPackageFileName(packageId, version))
                    .ToHashSet();

                if (!Directory.Exists(NotPublishedDirectoryName))
                {
                    Directory.CreateDirectory(NotPublishedDirectoryName);
                }

                var packageFolder = Environment.CurrentDirectory;
                var packageFiles = Directory.GetFiles(packageFolder, "*.nupkg");

                foreach (var packageFile in packageFiles)
                {
                    if (IsPublishedPackage(packageFile, publishedPackages))
                    {
                        continue;
                    }

                    MovePackageAsync(packageFile);
                }

                logger.LogInformation("Packages moved.");

                await NuGetDependencyFinder.FindDependenciesAsync(packageFolder, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return 1;
            }

            return 0;

            static bool IsPublishedPackage(string packagePath, HashSet<string> publishedPackages)
            {
                var packageFileName = Path.GetFileName(packagePath);
                return publishedPackages.Contains(packageFileName);
            }

            static void MovePackageAsync(string packagePath)
            {
                var packageFileName = Path.GetFileName(packagePath);
                File.Move(packagePath, Path.Combine(NotPublishedDirectoryName, packageFileName));
            }
        }
    }
}
