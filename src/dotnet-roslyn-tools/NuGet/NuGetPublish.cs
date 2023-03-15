// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;
using System.Diagnostics.Contracts;

using static Microsoft.RoslynTools.NuGet.Helpers;

namespace Microsoft.RoslynTools.NuGet
{
    internal class NuGetPublish
    {
        public const string RoslynRepo = "roslyn";
        public const string RoslynSdkRepo = "roslyn-sdk";

        internal static async Task<int> PublishAsync(string repoName, string source, string apiKey, bool unlisted, bool ignoreMissingPackage, ILogger logger)
        {
            try
            {
                string? version;

                var determinedVersion = repoName == RoslynRepo
                    ? TryDetermineRoslynPackageVersion(out version)
                    : TryDetermineRoslynSdkPackageVersion(out version);

                if (!determinedVersion)
                {
                    logger.LogError("Expected packages are missing. Unable to determine version.");
                    return 1;
                }

                Contract.Assert(version is not null);

                var packageIds = repoName == RoslynRepo
                    ? RoslynPackageIds
                    : RoslynSdkPackageIds;

                logger.LogInformation($"Publishing {version} packages...");

                var hasMissingPackageError = false;
                foreach (var packageId in packageIds)
                {
                    if (!File.Exists(GetPackageFileName(packageId, version)))
                    {
                        if (ignoreMissingPackage)
                        {
                            logger.LogInformation($"Missing package '{packageId}' is ignored.");
                        }
                        else
                        {
                            logger.LogError($"Required package '{packageId}' is missing");
                            hasMissingPackageError = true;
                        }
                    }
                }

                if (hasMissingPackageError)
                {
                    logger.LogInformation("Publication failed.");
                    return 1;
                }

                foreach (var packageId in packageIds)
                {
                    var result = await PublishPackageAsync(packageId, version);
                    if (result.ExitCode != 0)
                    {
                        logger.LogError($"Failed to publish '{packageId}'");
                        throw new InvalidOperationException(result.Output);
                    }
                    else
                    {
                        logger.LogInformation($"Package '{packageId}' published.");
                    }

                    if (unlisted)
                    {
                        await UnlistPackageAsync(packageId, version);
                        logger.LogInformation($"Package '{packageId}' unlisted.");
                    }
                }

                logger.LogInformation("Packages published.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return 1;
            }

            return 0;

            Task<ProcessResult> PublishPackageAsync(string packageId, string version)
            {
                return ProcessRunner.RunProcessAsync("dotnet", $"nuget push --source \"{source}\" --api-key \"{apiKey}\" \"{GetPackageFileName(packageId, version)}\"");
            }

            Task<ProcessResult> UnlistPackageAsync(string packageId, string version)
            {
                return ProcessRunner.RunProcessAsync("dotnet", $"nuget delete {packageId} {version} --source \"{source}\" --api-key \"{apiKey}\" --non-interactive");
            }
        }
    }
}
