// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public override bool Equals(object obj) => obj is BuildVersion version && Equals(version);
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

        public static explicit operator BuildVersion(Version version) => new(version.Build, version.Revision);
    }
}
