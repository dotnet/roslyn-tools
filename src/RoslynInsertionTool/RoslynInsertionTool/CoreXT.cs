// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        public JObject Components { get; }

        public CoreXT(string configPath, XDocument config, string componentsJSONPath, JObject components)
        {
            ConfigFilePath = configPath;
            ComponentsFilePath = componentsJSONPath;
            Config = config;
            Components = components;
        }

        public static CoreXT Load(string enlistmentRoot)
        {
            var defaultConfigPath = Path.Combine(enlistmentRoot, ".corext", "Configs", "default.config");
            var componentsJSONPath = Path.Combine(enlistmentRoot, ".corext", "Configs", "components.json");
            XDocument xDocument;
            JObject jsonDocument = null;

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
                    throw new IOException($"Unable to load config file from '{componentsJSONPath}'", e);
                }
            }

            return new CoreXT(defaultConfigPath, xDocument, componentsJSONPath, jsonDocument);
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
            try
            {
                using (var filestream = new FileStream(ComponentsFilePath, FileMode.Create, FileAccess.Write))
                using (var streamWriter = new StreamWriter(filestream))
                using (var writer = new JsonTextWriter(streamWriter)
                {
                    CloseOutput = true,
                    Indentation = 2,
                    Formatting = Formatting.Indented,
                })
                {
                    Components?.WriteTo(writer);
                }
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to save components file '{ComponentsFilePath}'", e);
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
            if (Components != null)
            {
                var componentJSON = Components["Components"][componentName];
                if (componentJSON == null)
                {
                    return false;
                }
                var componentFilename = (string)componentJSON["fileName"];
                var componentUri = new Uri((string)componentJSON["url"]);
                component = new Component(componentName, componentFilename, componentUri);
                return true;
            }

            return false;
        }

        public void UpdateComponent(Component component)
        {
            if (Components != null)
            {
                var componentJSON = Components["Components"][component.Name];
                componentJSON["fileName"] = component.Filename.ToString();
                componentJSON["url"] = component.Uri.ToString();
            }
        }
    }
}
