// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public Component WithUri(Uri newUri) => new(Name, Filename, newUri, Version);

        private static BuildVersion ParseBuildVersion(string uri)
        {
            try
            {
                var version = uri.Split('/').Last().Split(';').First();
                return BuildVersion.FromString(version);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
