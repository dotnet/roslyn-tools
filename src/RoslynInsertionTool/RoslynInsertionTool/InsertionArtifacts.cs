// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Insertion
{
    internal abstract class InsertionArtifacts
    {
        public abstract string RootDirectory { get; }

        public abstract string GetPackagesDirectory();
        public abstract string GetDependentAssemblyVersionsFile();
        public abstract string[] GetOptProfPropertyFiles();
    }
}
