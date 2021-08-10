// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;

namespace Roslyn.Insertion
{
    internal struct BuildVersion : IEquatable<BuildVersion>, IComparable<BuildVersion>
    {
        // 20160314
        public int Build { get; }

        // 1
        public int Revision { get; }

        public BuildVersion(int build, int revision)
        {
            Build = build;
            Revision = revision;
        }

        public override string ToString() => Build + "." + Revision;
        public bool Equals(BuildVersion other) => Build == other.Build && Revision == other.Revision;
        public override bool Equals(object obj) => obj is BuildVersion && Equals((BuildVersion)obj);
        public override int GetHashCode() => Build;

        public int CompareTo(BuildVersion other)
        {
            var result = Build.CompareTo(other.Build);
            return result == 0 ? Revision.CompareTo(other.Revision) : result;
        }

        public static bool operator <(BuildVersion x, BuildVersion y) => x.CompareTo(y) < 0;
        public static bool operator >(BuildVersion x, BuildVersion y) => x.CompareTo(y) > 0;
        public static bool operator <=(BuildVersion x, BuildVersion y) => x.CompareTo(y) <= 0;
        public static bool operator >=(BuildVersion x, BuildVersion y) => x.CompareTo(y) >= 0;
        public static bool operator ==(BuildVersion x, BuildVersion y) => x.Equals(y);
        public static bool operator !=(BuildVersion x, BuildVersion y) => !x.Equals(y);

        internal static BuildVersion FromTfsBuildNumber(string buildNumber, string roslynBuildName)
        {
            return FromString(buildNumber.Replace(roslynBuildName, string.Empty).Replace("_", string.Empty));
        }

        internal static BuildVersion FromString(string str)
        {
            string[] parts;
            if (str.IndexOf('.') >= 0)
            {
                parts = str.Split('.');
            }
            else if (str.IndexOf('-') >= 0)
            {
                parts = str.Split('-');
            }
            else
            {
                throw new FormatException($"BuildVersion should be in the form of 12345678.9 or 12345678-9");
            }

            return new BuildVersion(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        public static explicit operator BuildVersion(Version version) => new BuildVersion(version.Build, version.Revision);
    }
}
