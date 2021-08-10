﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace Roslyn.Insertion
{
    internal class CoreXT
    {
        private static Dictionary<string, string> ComponentToFileMap = null!;
        private static Dictionary<string, (string original, JObject document)> ComponentFileToDocumentMap = null!;
        private static HashSet<string> dirtyFiles = null!;

        private const string DefaultConfigPath = ".corext/Configs/default.config";
        private const string LegacyProjectPropsPath = "src/ConfigData/Packages/LegacyProjects.props";
        private const string ComponentsJsonPath = ".corext/Configs/components.json";

        private readonly string _defaultConfigOriginal;
        private readonly string? _legacyPropsOriginal;

        public XDocument ConfigDocument { get; }
        public XDocument? LegacyPropsDocument { get; }

        private CoreXT(string configOriginalText, string? legacyOriginalText)
        {
            _defaultConfigOriginal = configOriginalText;
            ConfigDocument = XDocument.Parse(configOriginalText, LoadOptions.None);

            _legacyPropsOriginal = legacyOriginalText;
            LegacyPropsDocument = legacyOriginalText is null ? null : XDocument.Parse(legacyOriginalText, LoadOptions.None);
        }

        public static async Task<CoreXT> Load(GitHttpClient gitClient, string commitId)
        {
            var vsRepoId = RoslynInsertionTool.VSRepoId;
            var vsBranch = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };

            using var defaultConfigStream = await gitClient.GetItemContentAsync(
                vsRepoId,
                DefaultConfigPath,
                download: true,
                versionDescriptor: vsBranch);
            var defaultConfigOriginal = await new StreamReader(defaultConfigStream).ReadToEndAsync();

            string? legacyPropsOriginal;
            try
            {
                using var legacyPropsStream = await gitClient.GetItemContentAsync(
                    vsRepoId,
                    LegacyProjectPropsPath,
                    download: true,
                    versionDescriptor: vsBranch);

                legacyPropsOriginal = await new StreamReader(legacyPropsStream).ReadToEndAsync();
            }
            catch (VssServiceException ex)
            {
                Console.WriteLine("Unable to load LegacyProjects.props. It will not be updated in this insertion.");
                Console.WriteLine(ex.Message);

                legacyPropsOriginal = null;
            }

            ComponentToFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ComponentFileToDocumentMap = new Dictionary<string, (string, JObject)>(StringComparer.OrdinalIgnoreCase);
            dirtyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await PopulateComponentJsonMaps(gitClient, commitId);

            return new CoreXT(defaultConfigOriginal, legacyPropsOriginal);
        }

        public (GitChange? defaultConfig, GitChange? legacyProps) SaveConfig()
        {
            var defaultConfig = RoslynInsertionTool.GetChangeOpt(DefaultConfigPath, _defaultConfigOriginal, toFullString(_defaultConfigOriginal, ConfigDocument));
            var legacyProps = RoslynInsertionTool.GetChangeOpt(LegacyProjectPropsPath, _legacyPropsOriginal, toFullString(_legacyPropsOriginal, LegacyPropsDocument));
            return (defaultConfig, legacyProps);

            static string? toFullString(string? original, XDocument? document)
            {
                if (original is null || document is null)
                {
                    return null;
                }

                var documentString = document.Declaration switch
                {
                    null => "",
                    var decl => decl.ToString() + "\n"
                } + document.ToString();
                if (original.EndsWith("\n"))
                {
                    documentString += "\n";
                }
                return documentString;
            }
        }

        public List<GitChange> SaveComponents()
        {
            var changes = new List<GitChange>();
            foreach (var kvp in ComponentFileToDocumentMap)
            {
                if (dirtyFiles.Contains(kvp.Key))
                {
                    var (original, doc) = kvp.Value;
                    if (doc is null)
                    {
                        continue;
                    }

                    // Preserve trailing newline if present
                    var newText = doc.ToString(Formatting.Indented) + (original.EndsWith("\n") ? "\n" : "");
                    if (RoslynInsertionTool.GetChangeOpt(kvp.Key, original, newText) is GitChange change)
                    {
                        changes.Add(change);
                    }
                }
            }

            return changes;
        }

        public XAttribute? GetDefaultConfigVersionAttribute(PackageInfo packageInfo)
        {
            return ConfigDocument.Root
                .Elements("packages")
                .Elements("package")
                .Where(p => p.Attribute("id")?.Value == packageInfo.PackageName)
                .Select(x => x.Attribute("version")).SingleOrDefault();
        }

        public XAttribute? GetLegacyPropsVersionAttributeOpt(PackageInfo packageInfo)
        {
            return LegacyPropsDocument?.Root
                .Elements("ItemGroup")
                .Elements("PackageReference")
                .Where(p => p.Attribute("Update")?.Value == packageInfo.PackageName)
                .Select(x => x.Attribute("Version")).SingleOrDefault();
        }

        public XElement GetClosestFollowingPackageElement(PackageInfo packageInfo)
        {
            return ConfigDocument.Root.
                Elements("packages").
                Elements("package").
                FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Compare(packageInfo.PackageName, p.Attribute("id").Value) < 0);
        }

        internal void AddNewPackage(PackageInfo packageInfo)
        {
            throw new NotSupportedException("Adding a new package is not supported until we also update for LegacyProjects.props");

#pragma warning disable CS0162 // Unreachable code detected
            var followingElement = GetClosestFollowingPackageElement(packageInfo);

            var package = new XElement(
                "package",
                new XAttribute("id", packageInfo.PackageName),
                new XAttribute("version", packageInfo.Version.ToString()),
                new XAttribute("link", $@"src\ExternalAPIs\{packageInfo.LibraryName}"),
                new XAttribute("tags", "exapis"));

            followingElement.AddAfterSelf(package);
#pragma warning restore CS0162 // Unreachable code detected
        }

        public void UpdatePackageVersion(PackageInfo packageInfo)
        {
            var versionAttribute = GetDefaultConfigVersionAttribute(packageInfo);
            versionAttribute?.SetValue(packageInfo.Version.ToString());

            var legacyPropsVersionAttribute = GetLegacyPropsVersionAttributeOpt(packageInfo);
            legacyPropsVersionAttribute?.SetValue(packageInfo.Version.ToString());
        }

        public static NuGetVersion GetPackageVersion(XAttribute versionAttribute)
        {
            return NuGetVersion.Parse(versionAttribute.Value);
        }

        public bool TryGetPackageVersion(PackageInfo packageInfo, out NuGetVersion? version)
        {
            var attribute = GetDefaultConfigVersionAttribute(packageInfo);
            if (attribute == null)
            {
                version = default;
                return false;
            }

            version = GetPackageVersion(attribute);
            return true;
        }

        public bool TryGetComponentByName(string componentName, out Component? component)
        {
            component = null;

            (_, JObject? componentDocument) = GetJsonDocumentForComponent(componentName);

            if (componentDocument == null)
            {
                return false;
            }

            var componentJSON = componentDocument["Components"][componentName];
            if (componentJSON == null)
            {
                return false;
            }

            var componentFilename = (string)componentJSON["fileName"];
            var componentUri = new Uri((string)componentJSON["url"]);
            var version = componentJSON.Value<string>("version"); // might not be present
            component = new Component(componentName, componentFilename, componentUri, version);
            return true;
        }

        public void UpdateComponent(Component component)
        {
            var (_, componentDocument) = GetJsonDocumentForComponent(component.Name);

            if (componentDocument != null)
            {
                var componentJSON = (JObject)componentDocument["Components"][component.Name];
                componentJSON["fileName"] = component.Filename;
                componentJSON["url"] = component.Uri.ToString();
                if (component.Version == null)
                {
                    // ensure no 'version' property is set in the JSON
                    var versionProperty = componentJSON.Property("version");
                    versionProperty?.Remove();
                }
                else
                {
                    // otherwise set or update the version
                    componentJSON["version"] = component.Version;
                }

                string componentFilePath = ComponentToFileMap[component.Name];
                dirtyFiles.Add(componentFilePath);
            }
        }

        private static async Task PopulateComponentJsonMaps(
            GitHttpClient gitClient,
            string commitId)
        {
            var (mainOriginal, mainComponentsJsonDocument) = await GetJsonDocumentForComponentsFile(gitClient, commitId, ComponentsJsonPath);
            if (mainComponentsJsonDocument != null)
            {
                ComponentFileToDocumentMap[ComponentsJsonPath] = (mainOriginal, mainComponentsJsonDocument);
                PopulateComponentToFileMapForFile(mainComponentsJsonDocument, ComponentsJsonPath);

                // Process sub components.json
                var imports = mainComponentsJsonDocument["Imports"];
                if (imports != null)
                {
                    foreach (var import in imports)
                    {
                        string subComponentFileName = (string)import;

                        if (!string.IsNullOrEmpty(subComponentFileName))
                        {
                            var componentsJSONPath = ".corext/Configs/" + subComponentFileName;
                            var (original, jDoc) = await GetJsonDocumentForComponentsFile(gitClient, commitId, componentsJSONPath);

                            if (jDoc != null && !ComponentFileToDocumentMap.ContainsKey(componentsJSONPath))
                            {
                                ComponentFileToDocumentMap[componentsJSONPath] = (original, jDoc);
                                PopulateComponentToFileMapForFile(jDoc, componentsJSONPath);
                            }
                        }
                    }
                }
            }
        }

        private static void PopulateComponentToFileMapForFile(JObject jDocument, string componentsJsonFileName)
        {
            if (jDocument != null && !string.IsNullOrEmpty(componentsJsonFileName))
            {
                var jComponents = (JObject)jDocument["Components"];

                if (jComponents != null)
                {
                    Dictionary<string, JToken> componentsMap = jComponents.ToObject<Dictionary<string, JToken>>();

                    if (componentsMap != null && componentsMap.Any())
                    {
                        foreach (var kvp in componentsMap)
                        {
                            if (!ComponentToFileMap.ContainsKey(kvp.Key))
                            {
                                ComponentToFileMap[kvp.Key] = componentsJsonFileName;
                            }
                        }
                    }
                }
            }
        }

        private static async Task<(string original, JObject document)> GetJsonDocumentForComponentsFile(
            GitHttpClient gitClient,
            string commitId,
            string componentsJSONPath)
        {
            var versionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };
            try
            {
                using var fileStream = await gitClient.GetItemContentAsync(RoslynInsertionTool.VSRepoId, path: componentsJSONPath, versionDescriptor: versionDescriptor);
                var original = await new StreamReader(fileStream).ReadToEndAsync();
                var jsonDocument = (JObject)JToken.Parse(original);
                return (original, jsonDocument);
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to load file from '{componentsJSONPath}'", e);
            }
        }

        private (string? original, JObject? document) GetJsonDocumentForComponent(string componentName)
        {
            (string?, JObject?) pair = (null, null);

            if (!string.IsNullOrEmpty(componentName))
            {
                string componentFileName;
                if (ComponentToFileMap.TryGetValue(componentName, out componentFileName))
                {
                    // ValueTuple is not covariant so we need to suppress the warning on 'pair'
                    ComponentFileToDocumentMap.TryGetValue(componentFileName, out pair!);
                }
            }

            return pair;
        }
    }
}
