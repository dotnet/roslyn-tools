// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Roslyn.Insertion
{
    internal struct SemanticVersion : IEquatable<SemanticVersion>
    {
        public Version Version { get; }
        public string Suffix { get; }

        public SemanticVersion(Version version, string suffix = "")
        {
            Debug.Assert(version != null);
            Debug.Assert(suffix.Length == 0 || suffix.StartsWith("-"));

            Version = version;
            Suffix = suffix;
        }

        public bool HasSuffix => Suffix.Length != 0;

        public override string ToString() => Version.ToString() + Suffix;
        public bool Equals(SemanticVersion other) => Version == other.Version && Suffix == other.Suffix;
        public override bool Equals(object obj) => obj is SemanticVersion && Equals((SemanticVersion)obj);
        public override int GetHashCode() => Version.GetHashCode() ^ Suffix.GetHashCode();
        public static bool operator ==(SemanticVersion x, SemanticVersion y) => x.Equals(y);
        public static bool operator !=(SemanticVersion x, SemanticVersion y) => !x.Equals(y);

        internal static SemanticVersion Parse(string str)
        {
            var dash = str.IndexOf('-');
            return dash >= 0 ? new SemanticVersion(Version.Parse(str.Substring(0, dash)), str.Substring(dash)) : new SemanticVersion(Version.Parse(str));
        }

        // -beta1-######-##
        internal BuildVersion GetSuffixBuildVersion()
        {
            var parts = Suffix.Split('-');
            return new BuildVersion(int.Parse(parts[parts.Length - 2]), int.Parse(parts[parts.Length - 1]));
        }
    }
}
