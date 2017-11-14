using System;

namespace Roslyn.Insertion
{
    internal static class VersionExtensions
    {
        public static Version ToFullVersion(this Version version) =>
            new Version(version.Major, version.Minor, version.Build == -1 ? 0 : version.Build, version.Revision == -1 ? 0 : version.Revision);
    }
}
