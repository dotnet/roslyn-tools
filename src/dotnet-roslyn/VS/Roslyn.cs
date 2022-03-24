// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.Roslyn.VS
{
    internal interface IProduct
    {
        Product Product { get; }

        string RepoBaseUrl { get; }
        string ComponentJsonFileName { get; }
        string ComponentName { get; }
        string BuildPipelineName { get; }
        string? PackageName { get; }
        string? ArtifactsFolderName { get; }
        string[] ArtifactsSubFolderNames { get; }
    }

    internal class Roslyn : IProduct
    {
        public Product Product => Product.Roslyn;

        public string RepoBaseUrl => "https://github.com/dotnet/roslyn";
        public string ComponentJsonFileName => @".corext\Configs\dotnetcodeanalysis-components.json";
        public string ComponentName => "Microsoft.CodeAnalysis.LanguageServices";
        public string BuildPipelineName => "dotnet-roslyn CI";
        public string? PackageName => "VS.ExternalAPIs.Roslyn";
        public string? ArtifactsFolderName => "PackageArtifacts";
        public string[] ArtifactsSubFolderNames => new[] { "PackageArtifacts/PreRelease", "PackageArtifacts/Release" };
    }

    internal class Razor : IProduct
    {
        public Product Product => Product.Razor;

        public string RepoBaseUrl => "https://github.com/dotnet/razor-tooling";
        public string ComponentJsonFileName => @".corext\Configs\aspnet-components.json";
        public string ComponentName => "Microsoft.VisualStudio.RazorExtension";
        public string BuildPipelineName => "razor-tooling-ci-official";
        public string? PackageName => null;
        public string? ArtifactsFolderName => null;
        public string[] ArtifactsSubFolderNames => Array.Empty<string>();
    }
}
