// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

            try
            {
                NuGetVersionUpdater.Run(Packages, OutputDirectory, isRelease);
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
