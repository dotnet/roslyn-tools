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
        private static Dictionary<string, JObject> ComponentFileToDocumentMap;
        private static HashSet<string> dirtyFiles;

        private const string DefaultConfigPath = ".corext/Configs/default.config";
        private const string ComponentsJsonPath = ".corext/Configs/components.json";

        public XDocument ConfigDocument { get; }

        public CoreXT(XDocument config)
        {
            ConfigDocument = config;
        }

        public static CoreXT Load(GitHttpClient gitClient, RoslynInsertionToolOptions options)
        {
            var vsRepoId = RoslynInsertionTool.VSRepoId;
            var vsBranch = new GitVersionDescriptor { VersionType = GitVersionType.Branch, Version = options.VisualStudioBranchName };

            var defaultConfigStream = gitClient.GetItemContentAsync(
                vsRepoId,
                DefaultConfigPath,
                download: true,
                versionDescriptor: vsBranch).Result;

            XDocument defaultConfigDocument = XDocument.Load(defaultConfigStream, LoadOptions.None);

            ComponentToFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ComponentFileToDocumentMap = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            dirtyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            PopulateComponentJsonMaps(gitClient, options);

            return new CoreXT(defaultConfigDocument);
        }

        public GitChange SaveConfig()
        {
            var change = new GitChange
            {
                ChangeType = VersionControlChangeType.Edit,
                Item = new GitItem { Path = DefaultConfigPath },
                NewContent = new ItemContent() { Content = ConfigDocument.ToFullString(), ContentType = ItemContentType.RawText }
            };

            return change;
        }

        public List<GitChange> SaveComponents()
        {
            var changes = new List<GitChange>();
            foreach (KeyValuePair<string, JObject> kvp in ComponentFileToDocumentMap)
            {
                if (dirtyFiles.Contains(kvp.Key))
                {
                    if (kvp.Value is null)
                    {
                        continue;
                    }

                    var change = new GitChange
                    {
                        ChangeType = VersionControlChangeType.Edit,
                        Item = new GitItem { Path = kvp.Key },
                        NewContent = new ItemContent()
                        {
                            // todo: indentation 2 spaces?
                            Content = kvp.Value.ToString(Formatting.Indented),
                            ContentType = ItemContentType.RawText
                        }
                    };

                    changes.Add(change);
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
            var currentVersion = GetPackageVersion(versionAttribute); // TODO: remove dead local
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

            JObject componentDocument = GetJsonDocumentForComponent(componentName);

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
            var componentDocument = GetJsonDocumentForComponent(component.Name);

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

        private static void PopulateComponentJsonMaps(GitHttpClient gitClient, RoslynInsertionToolOptions options)
        {
            var mainComponentsJsonDocument = GetJsonDocumentForComponentsFile(gitClient, options, ComponentsJsonPath);
            if (mainComponentsJsonDocument != null)
            {
                ComponentFileToDocumentMap[ComponentsJsonPath] = mainComponentsJsonDocument;
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
                            var componentsJSONPath = Path.Combine(".corext", "Configs", subComponentFileName);
                            var jDoc = GetJsonDocumentForComponentsFile(gitClient, options, componentsJSONPath);

                            if (jDoc != null && !ComponentFileToDocumentMap.ContainsKey(componentsJSONPath))
                            {
                                ComponentFileToDocumentMap[componentsJSONPath] = jDoc;
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

        private static JObject GetJsonDocumentForComponentsFile(
            GitHttpClient gitClient,
            RoslynInsertionToolOptions options,
            string componentsJSONPath)
        {
            JObject jsonDocument = null;

            // TODO: use gitClient
            if (File.Exists(componentsJSONPath))
            {
                try
                {
                    using (var filestream = new FileStream(componentsJSONPath, FileMode.Open, FileAccess.ReadWrite))
                    using (var streamReader = new StreamReader(filestream))
                    using (var reader = new JsonTextReader(streamReader))
                    {
                        jsonDocument = (JObject)JToken.ReadFrom(reader);
                    }
                }
                catch (Exception e)
                {
                    throw new IOException($"Unable to load file from '{componentsJSONPath}'", e);
                }
            }

            return jsonDocument;
        }

        private JObject GetJsonDocumentForComponent(string componentName)
        {
            JObject document = null;
            if (!string.IsNullOrEmpty(componentName))
            {
                string componentFileName;
                if (ComponentToFileMap.TryGetValue(componentName, out componentFileName))
                {
                    ComponentFileToDocumentMap.TryGetValue(componentFileName, out document);
                }
            }

            return document;
        }
    }
}
