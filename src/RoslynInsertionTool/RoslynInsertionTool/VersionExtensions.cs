// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;

namespace Roslyn.Insertion
{
    internal static class VersionExtensions
    {
        public static Version ToFullVersion(this Version version) =>
            new(version.Major, version.Minor, version.Build == -1 ? 0 : version.Build, version.Revision == -1 ? 0 : version.Revision);
    }
}
