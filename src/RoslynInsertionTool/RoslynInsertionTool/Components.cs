// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Insertion
{
    public class Component
    {
        public string Filename { get; }
        public string Name { get; }
        public Uri Uri { get; }
        public string Version { get; }

        public Component(string componentName, string componentFilename, Uri componentUri, string version)
        {
            Name = componentName;
            Filename = componentFilename;
            Uri = componentUri;
            Version = version;
        }

        public Component WithUri(Uri newUri) => new Component(Name, Filename, newUri, Version);
    }
}
