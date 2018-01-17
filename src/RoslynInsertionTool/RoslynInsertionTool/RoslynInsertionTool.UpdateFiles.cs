// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        internal static string GetDevDivInsertionFilePath(BuildVersion version, string relativePath)
        {
            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Master-Signed-Release\20160315.3\DevDivInsertionFiles\Roslyn\all.roslyn.locproj"
            return Path.Combine(GetBuildDirectory(version), "DevDivInsertionFiles", relativePath);
        }

        internal static string GetPackagesDirPath(BuildVersion version)
        {
            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Master-Signed-Release\20160315.3\DevDivPackages"
            var devDivPackagesPath = Path.Combine(GetBuildDirectory(version), "DevDivPackages");
            if (File.Exists(devDivPackagesPath))
            {
                return devDivPackagesPath;
            }

            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Project-System\DotNet-Project-System\20180111.1\packages"
            var packagesPath = Path.Combine(GetBuildDirectory(version), "packages");
            if (File.Exists(packagesPath))
            {
                return packagesPath;
            }

            throw new InvalidOperationException($"Unable to find packages path,  tried '{devDivPackagesPath}' and '{packagesPath}'");
        }

        internal static string GetBuildDirectory(BuildVersion version)
        {
            // used for local testing:
            if (Options.BuildDropPath.EndsWith(@"Binaries\Debug", StringComparison.OrdinalIgnoreCase) ||
                Options.BuildDropPath.EndsWith(@"Binaries\Release", StringComparison.OrdinalIgnoreCase))
            {
                return Options.BuildDropPath;
            }

            return Path.Combine(
                Options.BuildDropPath,
                Options.BuildQueueName,
                Path.GetFileName(Options.BranchName), // The folder under BranchName is just the last component of the name
                Options.BuildConfig,
                version.ToString());
        }

        private static async Task<bool> TryUpdateFileAsync(
            string filePath,
            BuildVersion version,
            bool onlyCopyIfFileDoesNotExistAtDestination,
            CancellationToken cancellationToken)
        {
            var destinationFilePath = Path.Combine(Options.EnlistmentPath, "src", filePath);
            var destinationDirectory = new FileInfo(destinationFilePath).Directory.FullName;
            var sourceFilePath = GetDevDivInsertionFilePath(version, filePath);
            var sourceDirectory = new FileInfo(sourceFilePath).Directory.FullName;
            var fileToCopy = Path.GetFileName(sourceFilePath);

            var copyArguments = onlyCopyIfFileDoesNotExistAtDestination
                ? "/xo /xn /xc"
                : string.Empty;

            var arguments = $@" ""{sourceDirectory}"" ""{destinationDirectory}"" ""{fileToCopy}"" {copyArguments}";
            var errorMessage = $"Unable to copy file from {sourceFilePath} to {destinationFilePath}";

            return await TryCopyAsync(arguments, destinationDirectory, errorMessage, cancellationToken);
        }

        private static async Task<bool> TryCopyAsync(string arguments, string workingDirectory, string errorMessage, CancellationToken cancellationToken)
        {
            var xcopyResult = await AsyncProcess.StartAsync(
                executable: "robocopy",
                arguments: arguments,
                lowPriority: false,
                workingDirectory: workingDirectory,
                captureOutput: true,
                isErrorCodeOk: exitCode => exitCode >= 0 && exitCode <= 7,
                onErrorDataReceived: s => Log.Error($"Copy files error: {s}"),
                onOutputDataReceived: s => Log.Info($"{s}"),
                cancellationToken: cancellationToken);
            foreach (var outputLine in xcopyResult.OutputLines)
            {
                Log.Info(outputLine);
            }

            // robocopy returns exit codes 0-16 (inclusive) where 0-7 are success, 8-15 are failure, and 16 is fatal error
            // however, we additionally need to handle negative exit codes for the case of `Process.Kill()`
            if (xcopyResult.ExitCode < 0 || xcopyResult.ExitCode > 7)
            {
                Log.Error(errorMessage);
                Log.Error(xcopyResult.ErrorLines);
                return false;
            }

            return true;
        }
    }
}
