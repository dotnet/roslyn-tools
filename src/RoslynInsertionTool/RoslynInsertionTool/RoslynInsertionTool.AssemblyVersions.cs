// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private const string VersionEqualsPrefix = "Version=";

        private static void UpdateAssemblyVersions(InsertionArtifacts artifacts)
        {
            var versionsUpdater = new VersionsUpdater(GetAbsolutePathForEnlistment(), WarningMessages);

            foreach (var nameAndVersion in ReadAssemblyVersions(artifacts.GetDependentAssemblyVersionsFile()))
            {
                versionsUpdater.UpdateComponentVersion(nameAndVersion.Key, nameAndVersion.Value);
            }

            versionsUpdater.Save();
        }

        private static IEnumerable<KeyValuePair<string, Version>> ReadAssemblyVersions(string path)
        {
            return from line in File.ReadAllLines(path)
                   let columns = line.Split(',')
                   let versionFull = columns[1].Trim()
                   let versionProper = versionFull.StartsWith(VersionEqualsPrefix) ? versionFull.Substring(VersionEqualsPrefix.Length) : versionFull
                   let version = Version.Parse(versionProper)
                   let fullVersion = new Version(version.Major, Math.Max(version.Minor, 0), Math.Max(version.Build, 0), Math.Max(version.Revision, 0))
                   select new KeyValuePair<string, Version>(columns[0].Trim(), fullVersion);
        }
    }
}
