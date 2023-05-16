// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Products;

internal class TypeScript : IProduct
{
    public string Name => "TypeScript";

    public string RepoHttpBaseUrl => "https://devdiv.visualstudio.com/DevDiv/_git/TypeScript-VS";
    public string RepoSshBaseUrl => "devdiv@vs-ssh.visualstudio.com:v3/devdiv/DevDiv/TypeScript-VS";
    public string GitUserName => "";
    public string GitEmail => "";

    public string ComponentJsonFileName => @".corext\Configs\components.json";
    public string ComponentName => "TypeScript_Tools";
    public string? PackageName => "VS.ExternalAPIs.TypeScript.SourceMapReader.dev15";
    public string? ArtifactsFolderName => null;
    public string[] ArtifactsSubFolderNames => Array.Empty<string>();

    public string? GetBuildPipelineName(string buildProjectName)
        => buildProjectName switch
        {
            "DevDiv" => "TypeScript-VS Signed Build",
            _ => null
        };
}
