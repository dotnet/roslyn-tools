// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Insertion
{
    internal static class VersionExtensions
    {
        public static Version ToFullVersion(this Version version) =>
            new(version.Major, version.Minor, version.Build == -1 ? 0 : version.Build, version.Revision == -1 ? 0 : version.Revision);
    }
}
