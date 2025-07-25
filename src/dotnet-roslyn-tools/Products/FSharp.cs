﻿// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Products;

internal class FSharp : IProduct
{
    public string Name => "F#";

    public string RepoHttpBaseUrl => "https://github.com/dotnet/fsharp";
    public string InternalRepoBaseUrl => "";

    public string RepoSshBaseUrl => "git@github.com:dotnet/fsharp.git";
    public string GitUserName => "";
    public string GitEmail => "";

    public string ComponentJsonFileName => @".corext\Configs\components.json";
    public string ComponentName => "Microsoft.FSharp";
    public string? VsPackageName => null;
    public string? VsPackagePropsFileName => null;
    public string? DartLabPipelineName => null;
    public string? PRValidationPipelineName => null;
    public string? ArtifactsFolderName => null;
    public string[] ArtifactsSubFolderNames => [];
    public string SdkPackageName => "Microsoft.FSharp.Compiler";


    public string? GetBuildPipelineName(string buildProjectName)
        => buildProjectName switch
        {
            "internal" => "fsharp-ci", // dnceng
            _ => null
        };
}
