// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.TeamFoundation.Build.WebApi;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        internal static string GetDevDivInsertionFilePath(string artifactsFolder, string relativePath)
        {
            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Master-Signed-Release\20160315.3\DevDivInsertionFiles\Roslyn\all.roslyn.locproj"
            return Path.Combine(artifactsFolder, "DevDivInsertionFiles", relativePath);
        }

        internal static string GetPackagesDirPath(string artifactsFolder)
        {
            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Master-Signed-Release\20160315.3\DevDivPackages"
            var devDivPackagesPath = Path.Combine(artifactsFolder, "DevDivPackages");
            if (Directory.Exists(devDivPackagesPath))
            {
                return devDivPackagesPath;
            }

            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Project-System\DotNet-Project-System\20180111.1\packages"
            var packagesPath = Path.Combine(artifactsFolder, "packages");
            if (Directory.Exists(packagesPath))
            {
                return packagesPath;
            }

            throw new InvalidOperationException($"Unable to find packages path,  tried '{devDivPackagesPath}' and '{packagesPath}'");
        }

        private static async Task<bool> TryUpdateFileAsync(
            string artifactsFolder,
            string filePath,
            BuildVersion version,
            bool onlyCopyIfFileDoesNotExistAtDestination,
            CancellationToken cancellationToken)
        {
            var destinationFilePath = Path.Combine(Options.EnlistmentPath, "src", filePath);
            var destinationDirectory = new FileInfo(destinationFilePath).Directory.FullName;
            var sourceFilePath = GetDevDivInsertionFilePath(artifactsFolder, filePath);
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
                onErrorDataReceived: s => Console.WriteLine($"Copy files error: {s}"),
                onOutputDataReceived: s => Console.WriteLine($"{s}"),
                cancellationToken: cancellationToken);
            foreach (var outputLine in xcopyResult.OutputLines)
            {
                Console.WriteLine(outputLine);
            }

            // robocopy returns exit codes 0-16 (inclusive) where 0-7 are success, 8-15 are failure, and 16 is fatal error
            // however, we additionally need to handle negative exit codes for the case of `Process.Kill()`
            if (xcopyResult.ExitCode < 0 || xcopyResult.ExitCode > 7)
            {
                Console.WriteLine(errorMessage);
                Console.WriteLine(xcopyResult.ErrorLines);
                return false;
            }

            return true;
        }
    }
}
