// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Linq;

namespace Roslyn.Insertion
{
    public class Component
    {
        public string Filename { get; }
        public string Name { get; }
        public Uri Uri { get; }
        public string Version { get; }
        internal BuildVersion BuildVersion { get; }

        public Component(string componentName, string componentFilename, Uri componentUri, string version)
        {
            Name = componentName;
            Filename = componentFilename;
            Uri = componentUri;
            Version = version;
            BuildVersion = ParseBuildVersion(Uri.ToString());
        }

        public Component WithUri(Uri newUri) => new Component(Name, Filename, newUri, Version);

        private static BuildVersion ParseBuildVersion(string uri)
        {
            try
            {
                var version = uri.Split('/').Last().Split(';').First();
                return BuildVersion.FromString(version);
            }
            catch (System.Exception)
            {
                return default;
            }

        }
    }
}
