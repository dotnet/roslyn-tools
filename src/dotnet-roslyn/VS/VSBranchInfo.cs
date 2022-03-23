// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Xml.Linq;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Roslyn.Utilities;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.Roslyn.VS;

internal static class VSBranchInfo
{
    public static async Task<int> GetInfoAsync(string branch, Product product, ILogger logger)
    {
        try
        {
            var client = new SecretClient(
                vaultUri: new Uri("https://managedlanguages.vault.azure.net"),
                credential: new DefaultAzureCredential(includeInteractiveCredentials: true));

            using var devdivConnection = new AzDOConnection("https://devdiv.visualstudio.com/DefaultCollection", "DevDiv", client, "vslsnap-vso-auth-token");
            using var dncengConnection = new AzDOConnection("https://dnceng.visualstudio.com/DefaultCollection", "internal", client, "vslsnap-build-auth-token");

            if (product is Product.Roslyn or Product.All)
            {
                WriteHeader($"Roslyn info from VS branch: {branch}");

                await WriteRoslynBuildInfo(branch, devdivConnection, dncengConnection);
            }

            if (product is Product.Razor or Product.All)
            {
                WriteHeader($"Razor info from VS branch: {branch}");

                await WriteRazorBuildInfo(branch, devdivConnection, dncengConnection);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred: {0}", ex.Message);
        }

        return 1;
    }

    private static async Task WriteRazorBuildInfo(string branch, AzDOConnection devdiv, AzDOConnection dnceng)
    {
        var buildNumber = await VisualStudioRepository.GetBuildNumberFromComponentJsonFileAsync(branch, devdiv, @".corext\Configs\aspnet-components.json", "Microsoft.VisualStudio.RazorExtension");

        // Try getting build info from dnceng first
        var builds = await dnceng.TryGetBuildsAsync("razor-tooling-ci-official", buildNumber);
        if (builds is null or { Count: 0 })
        {
            throw new Exception($"Couldn't find build for build number: {buildNumber}");
        }

        foreach (var build in builds)
        {
            WriteBuild(build);

            Console.WriteLine();
        }
    }

    private static async Task WriteRoslynBuildInfo(string branch, AzDOConnection devdiv, AzDOConnection dnceng)
    {
        var buildNumber = await VisualStudioRepository.GetBuildNumberFromComponentJsonFileAsync(branch, devdiv, @".corext\Configs\dotnetcodeanalysis-components.json", "Microsoft.CodeAnalysis.LanguageServices");
        var packageVersion = await GetRoslynPackageVersion(branch, devdiv);

        // Try getting build info from dnceng first
        var builds = await dnceng.TryGetBuildsAsync("dotnet-roslyn CI", buildNumber);
        var buildConnection = dnceng;
        if (builds == null || builds.Count == 0)
        {
            // otherwise fallback to devdiv, where things used to be
            builds = await devdiv.TryGetBuildsAsync("Roslyn-Signed", buildNumber);
            buildConnection = devdiv;
        }

        if (builds is null or { Count: 0 })
        {
            throw new Exception($"Couldn't find build for package version: {packageVersion}, build number: {buildNumber}");
        }

        foreach (var build in builds)
        {
            WriteNameAndValue("Package Version", packageVersion);
            WriteBuild(build);

            await WriteArtifactInfo(buildConnection, build);

            Console.WriteLine();
        }
    }

    private static async Task<string> GetRoslynPackageVersion(string branch, AzDOConnection devdiv)
    {
        var commit = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = branch };
        var vsRepository = await devdiv.GitClient.GetRepositoryAsync("DevDiv", "VS");

        using var defaultConfigStream = await devdiv.GitClient.GetItemContentAsync(
            vsRepository.Id,
            @".corext\Configs\default.config",
            download: true,
            versionDescriptor: commit);

        using var streamReader = new StreamReader(defaultConfigStream);
        var fileContents = await streamReader.ReadToEndAsync();
        var defaultConfig = XDocument.Parse(fileContents);

        var packageVersion = defaultConfig.Root?.Descendants("package").Where(p => p.Attribute("id")?.Value == "VS.ExternalAPIs.Roslyn").Select(p => p.Attribute("version")?.Value).FirstOrDefault();

        if (packageVersion is null)
        {
            throw new Exception($"Couldn't find the Roslyn external APIs pacakge for branch: {branch}");
        }

        return packageVersion;
    }

    // Inspired by Mitch Denny: https://dev.azure.com/mseng/AzureDevOps/_git/ArtifactTool?path=/src/ArtifactTool/Commands/PipelineArtifacts/PipelineArtifactDownloadCommand.cs&version=GBusers/midenn/fcs-integration&line=68&lineEnd=69&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
    // Note: He wishes not to have his name attached to it.
    private static async Task WriteArtifactInfo(AzDOConnection connection, Build build)
    {
        var artifact = await connection.BuildClient.GetArtifactAsync(build.Project.Id, build.Id, "PackageArtifacts");

        var (id, name) = ParseContainerId(artifact.Resource.Data);

        var projectId = (await connection.ProjectClient.GetProject(connection.BuildProjectName)).Id;

        var items = await connection.ContainerClient.QueryContainerItemsAsync(id, projectId, name);

        var links = from i in items
                    where i.ItemType == VisualStudio.Services.FileContainer.ContainerItemType.Folder
                    where i.Path == "PackageArtifacts/PreRelease" ||
                          i.Path == "PackageArtifacts/Release"
                    select (i.Path.Split('/').Last(), i.ContentLocation + "&%24format=zip&saveAbsolutePath=false"); // %24 == $

        WriteNamesAndValues("Packages", links);

        static (long id, string name) ParseContainerId(string resourceData)
        {
            // Example of resourceData: "#/7029766/artifacttool-alpine-x64-Debug"
            var segments = resourceData.Split('/');

            if (segments.Length == 3 && segments[0] == "#" && long.TryParse(segments[1], out var containerId))
            {
                return (containerId, segments[2]);
            }

            throw new Exception($"Resource data value '{resourceData}' was not in expected format.");
        }
    }

    private static void WriteBuild(Build build)
    {
        WriteNameAndValue("Build Number", build.BuildNumber);
        WriteNameAndValue("Commit SHA", build.SourceVersion);
        WriteNameAndValue("Source Branch", build.SourceBranch.Replace("refs/heads/", ""));
        WriteNameAndValue("Build", ((ReferenceLink)build.Links.Links["web"]).Href);
    }

    private static void WriteHeader(string branch)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{branch}:");
        Console.ResetColor();
    }

    private static void WriteNameAndValue(string name, string value, string indent = "  ")
    {
        Console.Write($"{indent}{name}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(value);
        Console.ResetColor();
    }

    private static void WriteNamesAndValues(string header, IEnumerable<(string, string)> values)
    {
        Console.WriteLine($"  {header}:");
        foreach (var (name, value) in values)
        {
            WriteNameAndValue(name, value, "    ");
        }
    }
}
