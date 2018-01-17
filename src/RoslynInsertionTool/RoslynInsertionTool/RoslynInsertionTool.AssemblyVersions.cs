// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static void UpdateAssemblyVersions(BuildVersion buildVersion)
        {
            var versionsUpdater = new VersionsUpdater(Log, GetAbsolutePathForEnlistment(), WarningMessages);

            foreach (var nameAndVersion in ReadAssemblyVersions(GetDevDivInsertionFilePath(buildVersion, "DependentAssemblyVersions.csv")))
            {
                versionsUpdater.UpdateComponentVersion(nameAndVersion.Key, nameAndVersion.Value);
            }

            versionsUpdater.Save();
        }

        private static IEnumerable<KeyValuePair<string, Version>> ReadAssemblyVersions(string path)
        {
            return from line in File.ReadAllLines(path)
                   let columns = line.Split(',')
                   let version = Version.Parse(columns[1])
                   let fullVersion = new Version(version.Major, Math.Max(version.Minor, 0), Math.Max(version.Build, 0), Math.Max(version.Revision, 0))
                   select new KeyValuePair<string, Version>(columns[0], fullVersion);
        }
    }
}
