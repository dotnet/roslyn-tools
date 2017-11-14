using System;

namespace Roslyn.Insertion
{
    public class Component
    {
        public string Filename { get; }
        public string Name { get; }
        public Uri Uri { get; }

        public Component(string componentName, string componentFilename, Uri componentUri)
        {
            Name = componentName;
            Filename = componentFilename;
            Uri = componentUri;
        }

        public Component WithUri(Uri newUri) => new Component(Name, Filename, newUri);
    }
}