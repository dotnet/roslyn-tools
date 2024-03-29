﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SignTool
{
    internal static class PathUtil
    {
        internal static bool IsVsix(string fileName) => Path.GetExtension(fileName).Equals(".vsix", StringComparison.OrdinalIgnoreCase);
        internal static bool IsNupkg(string fileName) => Path.GetExtension(fileName).Equals(".nupkg", StringComparison.OrdinalIgnoreCase);

        internal static bool IsZipContainer(string fileName) =>
            IsVsix(fileName) ||
            IsNupkg(fileName);

        internal static bool IsAssembly(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            return ext == ".exe" || ext == ".dll";
        }

        internal static bool IsAnyDirectorySeparator(char c) => c == '\\' || c == '/';

        internal static string NormalizeSeparators(string s)
        {
            if (!s.Contains("/"))
            {
                return s;
            }

            return s.Replace("/", "\\");
        }
    }
}
