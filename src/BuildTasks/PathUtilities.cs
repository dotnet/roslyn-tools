// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace RoslynTools.BuildTasks
{
    internal static class PathUtilities
    {
        public static bool EndsWithSeparator(this string path)
        {
            char last = path[path.Length - 1];
            return last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar;
        }

        public static string EndWithSeparator(this string path)
            => path.EndsWithSeparator() ? path : path + Path.DirectorySeparatorChar;

        public static string EndWithSeparator(this string path, char separator)
            => path.EndsWithSeparator() ? path : path + separator;
    }
}
