// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System.IO;
using System.Linq;

namespace Roslyn.Insertion
{
    internal abstract class InsertionArtifacts
    {
        internal abstract string RootDirectory { get; }

        public abstract string GetPackagesDirectory();
        public abstract string GetDependentAssemblyVersionsFile();
        public abstract string[] GetOptProfPropertyFiles();

        public string FindFilePath(string fileName)
        {
            return Directory.EnumerateFiles(RootDirectory, fileName, SearchOption.AllDirectories).SingleOrDefault();
        }
    }
}
