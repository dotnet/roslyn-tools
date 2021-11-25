using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json.Linq;

namespace VSBranchInfo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new SecretClient(
                 vaultUri: new Uri("https://roslyninfra.vault.azure.net:443"),
                 credential: new DefaultAzureCredential(includeInteractiveCredentials: true));

            using var devdivConnection = new AzDOConnection("https://devdiv.visualstudio.com/DefaultCollection", "DevDiv", "Roslyn-Signed", client, "vslsnap-vso-auth-token");
            using var dncengConnection = new AzDOConnection("https://dnceng.visualstudio.com/DefaultCollection", "internal", "dotnet-roslyn CI", client, "vslsnap-build-auth-token");

            if (args.Length == 0)
            {
                args = await GetDefaultVSBranches(devdivConnection);
            }

            foreach (var branch in args)
            {
                WriteHeader(branch);
                try
                {
                    await WriteRoslynBuildInfo(branch, devdivConnection, dncengConnection);
                }
                catch (Exception ex)
                {
                    WriteError(ex);
                }
            }
        }

        private static async Task<string[]> GetDefaultVSBranches(AzDOConnection devdiv)
        {
            Console.WriteLine("Finding branches in the VS repository. This may take a while...");
            Console.WriteLine("You can skip this specifying branches as command line arguments.");
            Console.WriteLine();

            var vsRepository = await devdiv.GitClient.GetRepositoryAsync("DevDiv", "VS");

            var branches = await devdiv.GitClient.GetBranchesAsync(vsRepository.Id);

            // Get the top 4 branches. In theory:
            //   1. main
            //   2. next version
            //   3. current version
            //   4. previous version
            return (from b in branches
                    where b.Name == "main" || b.Name.StartsWith("rel/d")
                    let ver = GetVersionNumber(b)
                    orderby ver descending
                    select b.Name).Take(4).ToArray();

            static Version GetVersionNumber(GitBranchStats b)
            {
                // Sort main to the top, always
                if (b.Name == "main")
                {
                    return new Version(999, 9999);
                }
                else if (Version.TryParse(b.Name.Substring(5), out var version))
                {
                    return version;
                }
                return new Version(0, 0);
            }
        }

        private static async Task WriteRoslynBuildInfo(string branch, AzDOConnection devdiv, AzDOConnection dnceng)
        {
            var (packageVersion, buildNumber) = await GetRoslynPackageInfo(branch, devdiv);

            // Try getting build info from dnceng first
            var builds = await TryGetBuilds(dnceng, buildNumber);
            var buildConnection = dnceng;
            if (builds == null || builds.Count == 0)
            {
                // otherwise fallback to devdiv, where things used to be
                builds = await TryGetBuilds(devdiv, buildNumber);
                buildConnection = devdiv;
            }

            if (builds is null or { Count: 0 })
            {
                throw new Exception($"Couldn't find build for package version: {packageVersion}, build number: {buildNumber}");
            }

            foreach (var build in builds)
            {
                WriteNameAndValue("Package Version", packageVersion);
                WriteNameAndValue("Commit Sha", build.SourceVersion);
                WriteNameAndValue("Source branch", build.SourceBranch.Replace("refs/heads/", ""));
                WriteNameAndValue("Build", ((ReferenceLink)build.Links.Links["web"]).Href);

                await WriteArtifactInfo(buildConnection, build);

                Console.WriteLine();
            }
        }

        private static async Task<(string packageVersion, string buildNumber)> GetRoslynPackageInfo(string branch, AzDOConnection devdiv)
        {
            var commit = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = branch };
            GitRepository vsRepository = await devdiv.GitClient.GetRepositoryAsync("DevDiv", "VS");

            using var componentsJsonStream = await devdiv.GitClient.GetItemContentAsync(
                vsRepository.Id,
                @".corext\Configs\dotnetcodeanalysis-components.json",
                download: true,
                versionDescriptor: commit);

            var fileContents = await new StreamReader(componentsJsonStream).ReadToEndAsync();
            var componentsJson = JObject.Parse(fileContents);

            var languageServicesUrlAndManifestName = componentsJson["Components"]?["Microsoft.CodeAnalysis.LanguageServices"]?["url"]?.ToString();

            var parts = languageServicesUrlAndManifestName?.Split(';');
            if (parts?.Length != 2)
            {
                throw new Exception($"Couldn't get URL and manifest. Got: {parts}");
            }

            if (!parts[1].EndsWith(".vsman"))
            {
                throw new Exception($"Couldn't get URL and manifest. Not a vsman file? Got: {parts}");
            }

            using var defaultConfigStream = await devdiv.GitClient.GetItemContentAsync(
                vsRepository.Id,
                @".corext\Configs\default.config",
                download: true,
                versionDescriptor: commit);

            fileContents = await new StreamReader(defaultConfigStream).ReadToEndAsync();
            var defaultConfig = XDocument.Parse(fileContents);

            var packageVersion = defaultConfig.Root?.Descendants("package").Where(p => p.Attribute("id")?.Value == "VS.ExternalAPIs.Roslyn").Select(p => p.Attribute("version")?.Value).FirstOrDefault();

            if (packageVersion is null)
            {
                throw new Exception($"Couldn't find the Roslyn external APIs pacakge for branch: {branch}");
            }

            var buildNumber = new Uri(parts[0]).Segments.Last();

            return (packageVersion, buildNumber);
        }

        private static async Task<List<Build>?> TryGetBuilds(AzDOConnection connection, string buildNumber)
        {
            try
            {
                var buildDefinition = (await connection.BuildClient.GetDefinitionsAsync(connection.BuildProjectName, name: connection.BuildDefinitionName)).Single();
                var builds = await connection.BuildClient.GetBuildsAsync(buildDefinition.Project.Id, definitions: new[] { buildDefinition.Id }, buildNumber: buildNumber);
                return builds;
            }
            catch
            {
                return null;
            }
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
                        where i.ItemType == Microsoft.VisualStudio.Services.FileContainer.ContainerItemType.Folder
                        where i.Path == "PackageArtifacts/PreRelease" ||
                              i.Path == "PackageArtifacts/Release"
                        select (i.Path.Split('/').Last(), i.ContentLocation + "&%24format=zip&saveAbsolutePath=false"); // %24 == $

            WriteNamesAndValues("Packages", links);

            static (long id, string name) ParseContainerId(string resourceData)
            {
                // Example of resourceData: "#/7029766/artifacttool-alpine-x64-Debug"
                var segments = resourceData.Split('/');

                long containerId;
                if (segments.Length == 3 && segments[0] == "#" && long.TryParse(segments[1], out containerId))
                {
                    return (containerId, segments[2]);
                }

                throw new Exception($"Resource data value '{resourceData}' was not in expected format.");
            }
        }

        private static void WriteError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
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
}
