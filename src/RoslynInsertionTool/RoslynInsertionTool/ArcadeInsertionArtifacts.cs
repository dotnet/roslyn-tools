// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace Roslyn.Insertion
{
    internal sealed class ArcadeInsertionArtifacts : InsertionArtifacts
    {
        public const string ArtifactName = "VSSetup";

        public override string RootDirectory { get; }

        public ArcadeInsertionArtifacts(string vsSetupDirectory)
        {
            RootDirectory = vsSetupDirectory;
        }

        public static bool TryCreateFromLocalBuild(string buildDirectory, out InsertionArtifacts artifacts)
        {
            if (buildDirectory.EndsWith(ArtifactName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Using artifacts provided in BuildDropPath: {buildDirectory}");
                artifacts = new ArcadeInsertionArtifacts(buildDirectory);
                return true;
            }

            artifacts = null;
            return false;
        }

        public override string GetPackagesDirectory()
        {
            var devDivPackagesPath = Path.Combine(RootDirectory, "DevDivPackages");
            if (Directory.Exists(devDivPackagesPath))
            {
                return devDivPackagesPath;
            }

            throw new InvalidOperationException($"Unable to find packages path, tried '{devDivPackagesPath}'");
        }

        public override string GetDependentAssemblyVersionsFile()
            => Path.Combine(RootDirectory, "DevDivPackages", "DependentAssemblyVersions.csv");

        public override string[] GetOptProfPropertyFiles()
            => Directory.EnumerateFiles(Path.Combine(RootDirectory, "Insertion", "OptProf"), "*.props", SearchOption.TopDirectoryOnly).ToArray();
    }
}
