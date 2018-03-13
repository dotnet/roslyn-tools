// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roslyn.Insertion
{
    internal class CoreXT
    {
        public string ConfigFilePath { get; }
        public string ComponentsFilePath { get; }
        public XDocument Config { get; }        
        private static Dictionary<string, string> ComponentToFileMap { get; set; }
        private static Dictionary<string, JObject> ComponentFileToDocumentMap { get; set; }
        private static HashSet<string> dirtyFiles;

        #region Public Methods

        public CoreXT(string configPath, XDocument config, string componentsJSONPath)
        {
            ConfigFilePath = configPath;
            ComponentsFilePath = componentsJSONPath;
            Config = config;
        }

        public static CoreXT Load(string enlistmentRoot)
        {
            var defaultConfigPath = Path.Combine(enlistmentRoot, ".corext", "Configs", "default.config");
            var componentsJSONPath = Path.Combine(enlistmentRoot, ".corext", "Configs", "components.json");
            XDocument xDocument;
            ComponentToFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ComponentFileToDocumentMap = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            dirtyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var fileStream = new FileStream(defaultConfigPath, FileMode.Open, FileAccess.ReadWrite))
                {
                    xDocument = XDocument.Load(fileStream, LoadOptions.None);
                }
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to load config file from '{defaultConfigPath}'", e);
            }

            PopulateComponentJsonMaps(componentsJSONPath, enlistmentRoot);

            return new CoreXT(defaultConfigPath, xDocument, componentsJSONPath);
        }
        
        public void SaveConfig()
        {
            try
            {
                Config.Save(ConfigFilePath, SaveOptions.None);
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to save config file '{ConfigFilePath}'", e);
            }
        }

        public void SaveComponents()
        {
            if (ComponentFileToDocumentMap.Any())
            {
                foreach (KeyValuePair<string, JObject> kvp in ComponentFileToDocumentMap)
                {
                    if (dirtyFiles.Contains(kvp.Key))
                    {
                        try
                        {
                            using (var filestream = new FileStream(kvp.Key, FileMode.Create, FileAccess.Write))
                            using (var streamWriter = new StreamWriter(filestream))
                            using (var writer = new JsonTextWriter(streamWriter)
                            {
                                CloseOutput = true,
                                Indentation = 2,
                                Formatting = Formatting.Indented,
                            })
                            {
                                kvp.Value?.WriteTo(writer);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new IOException($"Unable to save components file '{kvp.Key}'", e);
                        }
                    }
                }
            }
        }

        public XAttribute GetVersionAttribute(PackageInfo packageInfo)
        {
            return Config.Root
                .Elements("packages")
                .Elements("package")
                .Where(p => p.Attribute("id")?.Value == packageInfo.PackageName)
                .Select(x => x.Attribute("version")).SingleOrDefault();
        }

        public XElement GetClosestFollowingPackageElement(PackageInfo packageInfo)
        {
            return Config.Root.
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
            var currentVersion = GetPackageVersion(versionAttribute);
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
            component = new Component(componentName, componentFilename, componentUri);
            return true;
        }

        public void UpdateComponent(Component component)
        {
            var componentDocument = GetJsonDocumentForComponent(component.Name);

            if (componentDocument != null)
            {
                var componentJSON = componentDocument["Components"][component.Name];
                componentJSON["fileName"] = component.Filename;
                componentJSON["url"] = component.Uri.ToString();

                string componentFilePath = ComponentToFileMap[component.Name];
                if(!dirtyFiles.Contains(componentFilePath))
                {
                    dirtyFiles.Add(componentFilePath);
                }
            }
        }

#endregion

        #region Private Methods

        private static void PopulateComponentJsonMaps(string mainComponentsJsonPath, string enlistmentRoot)
        {
            var mainComponentsJsonDocument = GetJsonDocumentForComponentsFile(mainComponentsJsonPath);

            if (mainComponentsJsonDocument != null)
            {
                ComponentFileToDocumentMap[mainComponentsJsonPath] = mainComponentsJsonDocument;
                PopulateComponentToFileMapForFile(mainComponentsJsonDocument, mainComponentsJsonPath);

                // Process sub components.json
                var imports = mainComponentsJsonDocument["Imports"];
                if (imports != null)
                {
                    foreach (var import in imports)
                    {
                        string subComponentFileName = (string)import;

                        if (!string.IsNullOrEmpty(subComponentFileName))
                        {
                            var componentsJSONPath = Path.Combine(enlistmentRoot, ".corext", "Configs", subComponentFileName);
                            var jDoc = GetJsonDocumentForComponentsFile(componentsJSONPath);

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

        private static JObject GetJsonDocumentForComponentsFile(string componentsJSONPath)
        {
            JObject jsonDocument = null;

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

        #endregion
    }
}
