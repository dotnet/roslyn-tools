// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Roslyn.Tools
{
    public sealed class UpdatePackageVersionTask : Task
    {
        [Required]
        public string VersionKind { get; set; }

        [Required]
        public string[] Packages { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        public bool ExactVersions { get; set; }

        public bool AllowPreReleaseDependencies { get; set; }

        public override bool Execute()
        {
            ExecuteImpl();
            return !Log.HasLoggedErrors;
        }

        private void ExecuteImpl()
        {
            bool isRelease;
            if (StringComparer.OrdinalIgnoreCase.Equals(VersionKind, "release"))
            {
                isRelease = true;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(VersionKind, "prerelease"))
            {
                isRelease = false;
            }
            else
            {
                Log.LogError($"Invalid value for task argument {nameof(VersionKind)}: '{VersionKind}'. Specify 'release' or 'prerelease'.");
                return;
            }

            var preReleaseDependencies = new List<string>();

            try
            {
                NuGetVersionUpdater.Run(Packages, OutputDirectory, isRelease, ExactVersions, allowPreReleaseDependency: (packageId, dependencyId, dependencyVersion) =>
                {
                    if (AllowPreReleaseDependencies)
                    {
                        Log.LogMessage(MessageImportance.High, $"Package '{packageId}' depends on a pre-release package '{dependencyId}, {dependencyVersion}'");
                        preReleaseDependencies.Add($"{dependencyId}, {dependencyVersion}");
                        return true;
                    }

                    return false;
                });

                if (isRelease)
                {
                    File.WriteAllLines(Path.Combine(OutputDirectory, "PreReleaseDependencies.txt"), preReleaseDependencies.Distinct());
                }
            }
            catch (AggregateException e)
            {
                foreach (var inner in e.InnerExceptions)
                {
                    Log.LogErrorFromException(inner);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
            }

        }
    }
}
