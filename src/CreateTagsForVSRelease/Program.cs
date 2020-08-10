using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using LibGit2Sharp;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CreateTagsForVSRelease
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new SecretClient(
                vaultUri: new Uri("https://roslyninfra.vault.azure.net:443"),
                credential: new DefaultAzureCredential(includeInteractiveCredentials: true));

            var azureDevOpsSecret = await client.GetSecretAsync("vslsnap-vso-auth-token");
            using var connection = new VssConnection(
                new Uri("https://devdiv.visualstudio.com/DefaultCollection"),
                new WindowsCredential(new NetworkCredential("vslsnap", azureDevOpsSecret.Value.Value)));

            using var gitClient = await connection.GetClientAsync<GitHttpClient>();
            using var buildClient = await connection.GetClientAsync<BuildHttpClient>();

            var visualStudioReleases = await GetVisualStudioReleasesAsync(gitClient);
            var roslynRepository = new Repository(args[0]);
            var existingTags = roslynRepository.Tags.ToImmutableArray();

            foreach (var visualStudioRelease in visualStudioReleases)
            {
                var roslynTagName = TryGetRoslynTagName(visualStudioRelease);

                if (roslynTagName != null)
                {
                    if (!existingTags.Any(t => t.FriendlyName == roslynTagName))
                    {
                        Console.WriteLine($"Tag {roslynTagName} is missing.");

                        var roslynBuild = await TryGetRoslynBuildForReleaseAsync(visualStudioRelease, gitClient, buildClient);

                        if (roslynBuild != null)
                        {
                            Console.WriteLine($"Tagging {roslynBuild.CommitSha} as {roslynTagName}.");

                            string message = $"Build Branch: {roslynBuild.SourceBranch}\r\nInternal ID: {roslynBuild.BuildId}\r\nInternal VS ID: {visualStudioRelease.BuildId}\r\nPackage ID: {roslynBuild.NugetPackageVersion}";

                            roslynRepository.ApplyTag(roslynTagName, roslynBuild.CommitSha, new Signature("dotnet bot", "dotnet-bot@microsoft.com", when: visualStudioRelease.CreationTime), message);
                        }
                        else
                        {
                            Console.WriteLine($"Unable to find the build for {roslynTagName}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Tag {roslynTagName} already exists.");
                    }
                }
            }
        }

        private static async Task<RoslynBuildInformation?> TryGetRoslynBuildForReleaseAsync(VisualStudioVersion release, GitHttpClient gitClient, BuildHttpClient buildClient)
        {
            var commit = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = release.CommitSha };
            GitRepository vsRepository = await GetVSRepositoryAsync(gitClient);

            using var componentsJsonStream = await gitClient.GetItemContentAsync(
                vsRepository.Id,
                @".corext\Configs\dotnetcodeanalysis-components.json",
                download: true,
                versionDescriptor: commit);

            var fileContents = await new StreamReader(componentsJsonStream).ReadToEndAsync();
            var componentsJson = JObject.Parse(fileContents);

            var languageServicesUrlAndManifestName = (string)componentsJson["Components"]["Microsoft.CodeAnalysis.LanguageServices"]["url"];

            var parts = languageServicesUrlAndManifestName.Split(';');
            if (parts.Length != 2)
            {
                return null;
            }

            if (!parts[1].EndsWith(".vsman"))
            {
                return null;
            }

            using var defaultConfigStream = await gitClient.GetItemContentAsync(
                vsRepository.Id,
                @".corext\Configs\default.config",
                download: true,
                versionDescriptor: commit);

            fileContents = await new StreamReader(defaultConfigStream).ReadToEndAsync();
            var defaultConfig = XDocument.Parse(fileContents);

            var packageVersion = defaultConfig.Root.Descendants("package").Where(p => p.Attribute("id")?.Value == "VS.ExternalAPIs.Roslyn").Select(p => p.Attribute("version")?.Value).FirstOrDefault();

            var buildNumber = new Uri(parts[0]).Segments.Last();

            var buildDefinition = (await buildClient.GetDefinitionsAsync(vsRepository.ProjectReference.Id, name: "Roslyn-Signed")).Single();
            var build = (await buildClient.GetBuildsAsync(buildDefinition.Project.Id, definitions: new[] { buildDefinition.Id }, buildNumber: buildNumber)).SingleOrDefault();

            if (build == null)
            {
                return null;
            }

            var buildId = buildDefinition.Name + "_" + build.BuildNumber;

            return new RoslynBuildInformation(commitSha: build.SourceVersion, build.SourceBranch.Replace("refs/heads/", ""), buildId, packageVersion);
        }

        private static async Task<ImmutableArray<VisualStudioVersion>> GetVisualStudioReleasesAsync(GitHttpClient gitClient)
        {
            GitRepository vsRepository = await GetVSRepositoryAsync(gitClient);
            var tags = await gitClient.GetRefsAsync(vsRepository.Id, filterContains: "release/vs", peelTags: true);

            var builder = ImmutableArray.CreateBuilder<VisualStudioVersion>();

            foreach (var tag in tags)
            {
                const string tagPrefix = "refs/tags/release/vs/";

                if (!tag.Name.StartsWith(tagPrefix))
                {
                    continue;
                }

                string[] parts = tag.Name.Substring(tagPrefix.Length).Split('-');

                if (parts.Length != 1 && parts.Length != 2)
                {
                    continue;
                }

                var isDottedVersionRegex = new Regex("^[0-9]+(\\.[0-9]+)*$");

                if (!isDottedVersionRegex.IsMatch(parts[0]))
                {
                    continue;
                }

                // If there is no peeled object, it means it's a simple tag versus an annotated tag; those aren't usually how
                // VS releases are tagged so skip it
                if (tag.PeeledObjectId == null)
                {
                    continue;
                }

                var annotatedTag = await gitClient.GetAnnotatedTagAsync(vsRepository.ProjectReference.Id, vsRepository.Id, tag.ObjectId);
                if (annotatedTag == null)
                {
                    continue;
                }

                JObject buildInformation;

                try
                {
                    buildInformation = JObject.Parse(annotatedTag.Message);
                }
                catch
                {
                    continue;
                }

                // It's not entirely clear to me how this format was chosen, but for consistency with old tags, we'll keep it
                string buildId = buildInformation["Branch"].ToString().Replace("/", ".") + "-" + buildInformation["BuildNumber"];

                if (parts.Length == 2)
                {
                    const string previewPrefix = "preview.";

                    if (!parts[1].StartsWith(previewPrefix))
                    {
                        continue;
                    }

                    string possiblePreviewVersion = parts[1].Substring(previewPrefix.Length);

                    if (!isDottedVersionRegex.IsMatch(possiblePreviewVersion))
                    {
                        continue;
                    }

                    builder.Add(new VisualStudioVersion(parts[0], possiblePreviewVersion, tag.PeeledObjectId, annotatedTag.TaggedBy.Date, buildId));
                }
                else
                {
                    builder.Add(new VisualStudioVersion(parts[0], previewVersion: null, tag.PeeledObjectId, annotatedTag.TaggedBy.Date, buildId));
                }
            }

            return builder.ToImmutable();
        }

        private static async Task<GitRepository> GetVSRepositoryAsync(GitHttpClient gitClient)
        {
            return await gitClient.GetRepositoryAsync("DevDiv", "VS");
        }

        private static string? TryGetRoslynTagName(VisualStudioVersion release)
        {
            string tag = "Visual-Studio-";

            if (release.MainVersion.StartsWith("16."))
            {
                tag += "2019-";
            }
            else
            {
                // We won't worry about tagging earlier things than VS2019 releases for now
                return null;
            }

            tag += "Version-" + release.MainVersion;

            if (release.PreviewVersion != null)
            {
                tag += "-Preview-" + release.PreviewVersion;
            }

            return tag;
        }
    }
}
