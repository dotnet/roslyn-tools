// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Roslyn.Insertion
{
    internal sealed class ArcadeInsertionArtifacts : InsertionArtifacts
    {
        private readonly string _vsSetupDirectory;

        public ArcadeInsertionArtifacts(string vsSetupDirectory)
        {
            _vsSetupDirectory = vsSetupDirectory;
        }

        internal static bool TryCreateFromLocalBuild(string buildDirectory, out InsertionArtifacts artifacts)
        {
            if (buildDirectory.EndsWith(@"artifacts\VSSetup\Debug", StringComparison.OrdinalIgnoreCase) ||
                buildDirectory.EndsWith(@"artifacts\VSSetup\Release", StringComparison.OrdinalIgnoreCase))
            {
                artifacts = new ArcadeInsertionArtifacts(buildDirectory);
                return true;
            }

            artifacts = null;
            return false;
        }

        public override string GetPackagesDirectory()
        {
            var devDivPackagesPath = Path.Combine(_vsSetupDirectory, "DevDivPackages");
            if (Directory.Exists(devDivPackagesPath))
            {
                return devDivPackagesPath;
            }

            throw new InvalidOperationException($"Unable to find packages path, tried '{devDivPackagesPath}'");
        }

        public override string GetDependentAssemblyVersionsFile()
            => Path.Combine(_vsSetupDirectory, "DevDivPackages", "DependentAssemblyVersions.csv");
    }
}
