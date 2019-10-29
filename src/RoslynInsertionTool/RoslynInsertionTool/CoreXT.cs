// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roslyn.Insertion
{
    internal class CoreXT
    {
        private static Dictionary<string, string> ComponentToFileMap;
        private static Dictionary<string, (string original, JObject document)> ComponentFileToDocumentMap;
        private static HashSet<string> dirtyFiles;

        private const string DefaultConfigPath = ".corext/Configs/default.config";
        private const string ComponentsJsonPath = ".corext/Configs/components.json";
        private readonly string _defaultConfigOriginal;

        public XDocument ConfigDocument { get; }

        public CoreXT(string configOriginalText)
        {
            _defaultConfigOriginal = configOriginalText;
            ConfigDocument = XDocument.Parse(configOriginalText, LoadOptions.PreserveWhitespace);
        }

        public static CoreXT Load(GitHttpClient gitClient, string commitId)
        {
            var vsRepoId = RoslynInsertionTool.VSRepoId;
            var vsBranch = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };

            var defaultConfigStream = gitClient.GetItemContentAsync(
                vsRepoId,
                DefaultConfigPath,
                download: true,
                versionDescriptor: vsBranch).Result;

            var defaultConfigOriginal = new StreamReader(defaultConfigStream).ReadToEnd();

            ComponentToFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ComponentFileToDocumentMap = new Dictionary<string, (string, JObject)>(StringComparer.OrdinalIgnoreCase);
            dirtyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            PopulateComponentJsonMaps(gitClient, commitId);

            return new CoreXT(defaultConfigOriginal);
        }

        public GitChange SaveConfigOpt()
        {
            return RoslynInsertionTool.GetChangeOpt(DefaultConfigPath, _defaultConfigOriginal, ConfigDocument.ToFullString());
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

        public XAttribute GetVersionAttribute(PackageInfo packageInfo)
        {
            return ConfigDocument.Root
                .Elements("packages")
                .Elements("package")
                .Where(p => p.Attribute("id")?.Value == packageInfo.PackageName)
                .Select(x => x.Attribute("version")).SingleOrDefault();
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
            var followingElement = GetClosestFollowingPackageElement(packageInfo);

            var package = new XElement(
                "package",
                new XAttribute("id", packageInfo.PackageName),
                new XAttribute("version", packageInfo.Version.ToString()),
                new XAttribute("link", $@"src\ExternalAPIs\{packageInfo.LibraryName}"),
                new XAttribute("tags", "exapis"));

            followingElement.AddAfterSelf(package);
        }

        public void UpdatePackageVersion(PackageInfo packageInfo)
        {
            var versionAttribute = GetVersionAttribute(packageInfo);
            versionAttribute.SetValue(packageInfo.Version.ToString());
        }

        public static SemanticVersion GetPackageVersion(XAttribute versionAttribute)
        {
            return SemanticVersion.Parse(versionAttribute.Value);
        }

        public bool TryGetPackageVersion(PackageInfo packageInfo, out SemanticVersion version)
        {
            var attribute = GetVersionAttribute(packageInfo);
            if (attribute == null)
            {
                version = default;
                return false;
            }

            version = GetPackageVersion(attribute);
            return true;
        }

        public bool TryGetComponentByName(string componentName, out Component component)
        {
            component = null;

            (string _, JObject componentDocument) = GetJsonDocumentForComponent(componentName);

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

        private static void PopulateComponentJsonMaps(
            GitHttpClient gitClient,
            string commitId)
        {
            var (mainOriginal, mainComponentsJsonDocument) = GetJsonDocumentForComponentsFile(gitClient, commitId, ComponentsJsonPath);
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
                            var (original, jDoc) = GetJsonDocumentForComponentsFile(gitClient, commitId, componentsJSONPath);

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

        private static (string original, JObject document) GetJsonDocumentForComponentsFile(
            GitHttpClient gitClient,
            string commitId,
            string componentsJSONPath)
        {
            JObject jsonDocument = null;
            string original = null;

            var versionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };
            try
            {
                using (var filestream = gitClient.GetItemContentAsync(RoslynInsertionTool.VSRepoId, path: componentsJSONPath, versionDescriptor: versionDescriptor).Result)
                using (var streamReader = new StreamReader(filestream))
                {
                    original = streamReader.ReadToEnd();
                    jsonDocument = (JObject)JToken.Parse(original);
                }
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to load file from '{componentsJSONPath}'", e);
            }

            return (original, jsonDocument);
        }

        private (string original, JObject document) GetJsonDocumentForComponent(string componentName)
        {
            (string, JObject) pair = (null, null);

            if (!string.IsNullOrEmpty(componentName))
            {
                string componentFileName;
                if (ComponentToFileMap.TryGetValue(componentName, out componentFileName))
                {
                    ComponentFileToDocumentMap.TryGetValue(componentFileName, out pair);
                }
            }

            return pair;
        }
    }
}
