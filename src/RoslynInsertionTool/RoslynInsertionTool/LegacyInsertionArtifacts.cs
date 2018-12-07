// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Roslyn.Insertion
{
    internal sealed class LegacyInsertionArtifacts : InsertionArtifacts
    {
        private readonly string _binariesDirectory;

        public LegacyInsertionArtifacts(string binariesDirectory)
        {
            _binariesDirectory = binariesDirectory;
        }

        internal static bool TryCreateFromLocalBuild(string buildDirectory, out InsertionArtifacts artifacts)
        {
            if (buildDirectory.EndsWith(@"Binaries\Debug", StringComparison.OrdinalIgnoreCase) ||
                buildDirectory.EndsWith(@"Binaries\Release", StringComparison.OrdinalIgnoreCase))
            {
                artifacts = new LegacyInsertionArtifacts(buildDirectory);
                return true;
            }

            artifacts = null;
            return false;
        }

        public override string GetPackagesDirectory()
        {
            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Master-Signed-Release\20160315.3\DevDivPackages"
            var devDivPackagesPath = Path.Combine(_binariesDirectory, "DevDivPackages");
            if (Directory.Exists(devDivPackagesPath))
            {
                return devDivPackagesPath;
            }

            // For example: "\\cpvsbuild\drops\Roslyn\Roslyn-Project-System\DotNet-Project-System\20180111.1\packages"
            var packagesPath = Path.Combine(_binariesDirectory, "packages");
            if (Directory.Exists(packagesPath))
            {
                return packagesPath;
            }

            throw new InvalidOperationException($"Unable to find packages path, tried '{devDivPackagesPath}' and '{packagesPath}'");
        }

        public override string GetDependentAssemblyVersionsFile()
            => Path.Combine(_binariesDirectory, "DevDivInsertionFiles", "DependentAssemblyVersions.csv");
    }
}
