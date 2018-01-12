// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NLog;

namespace Roslyn.Insertion
{
    internal sealed class VersionsUpdater
    {
        public ILogger Log { get; }
        public string EnlistmentRoot { get; }
        public List<string> WarningMessages { get; }

        private const string ConfigPath = @"src\VSSDK\VSIntegration\IsoShell\Templates\VSShellTemplate\VSShellIso\VSShellStubExe\Stub.exe.config";
        private readonly XDocument _configXml;
        private readonly string _configFullPath;

        private const string VersionsPath = @"src\ProductData\versions.xml";
        private readonly XDocument _versionsXml;
        private readonly string _versionsXmlFullPath;

        private const string VersionsTemplatePath = @"src\ProductData\AssemblyVersions.tt";
        private string _versionsTemplateContent;
        private readonly string _versionsTemplateFullPath;

        public VersionsUpdater(ILogger log, string enlistmentRoot, List<string> warningMessages)
        {
            Log = log;
            EnlistmentRoot = enlistmentRoot;
            WarningMessages = warningMessages;

            // this is a template which can't currently be templated:
            _configFullPath = Path.Combine(enlistmentRoot, ConfigPath);
            _configXml = XDocument.Load(_configFullPath);

            _versionsXmlFullPath = Path.Combine(EnlistmentRoot, VersionsPath);
            _versionsXml = XDocument.Load(_versionsXmlFullPath);

            // template defining version variables that flow to .config.tt files:
            _versionsTemplateFullPath = Path.Combine(EnlistmentRoot, VersionsTemplatePath);
            _versionsTemplateContent = File.ReadAllText(_versionsTemplateFullPath);
        }

        public static IEnumerable<string> RelativeFilePaths
        {
            get
            {
                yield return ConfigPath;
                yield return VersionsPath;
                yield return VersionsTemplatePath;
            }
        }

        public void Save()
        {
            _configXml.Save(_configFullPath);
            _versionsXml.Save(_versionsXmlFullPath);
            File.WriteAllText(_versionsTemplateFullPath, _versionsTemplateContent);
        }

        public void UpdateComponentVersion(string assemblyName, Version newVersion)
        {
            UpdateConfigFile(assemblyName, newVersion);
            UpdateVersionsFile(assemblyName, newVersion);
            UpdateAssemblyVersionsFile(assemblyName, newVersion);
        }

        private void UpdateConfigFile(string assemblyName, Version newVersion)
        {
            const string ns = "urn:schemas-microsoft-com:asm.v1";

            var fullPath = _configFullPath;

            var dependentAssemblies = _configXml?.Root?.
                Element("runtime")?.
                Element(XName.Get("assemblyBinding", ns))?.
                Elements(XName.Get("dependentAssembly", ns));

            if (dependentAssemblies == null)
            {
                throw new InvalidDataException($"File '{fullPath}' doesn't have expected format.");
            }

            var dependentAssembly = dependentAssemblies.FirstOrDefault(n => n.Element(XName.Get("assemblyIdentity", ns)).Attribute("name").Value == assemblyName);
            if (dependentAssembly == null)
            {
                // ok, not all configs contain all redirects
                return;
            }

            var bindingRedirect = dependentAssembly.Element(XName.Get("bindingRedirect", ns));
            var oldVersionAttribute = bindingRedirect?.Attribute("oldVersion");
            var newVersionAttribute = bindingRedirect?.Attribute("newVersion");

            if (!TryParseVersionRange(oldVersionAttribute?.Value, out var oldVersionInAttributeLow, out var oldVersionInAttributeHigh) ||
                oldVersionInAttributeLow != new Version(0, 0, 0, 0))
            {
                throw new InvalidDataException($"The value of attribute oldVersion '{oldVersionAttribute?.Value}' doesn't have the expected format: '0.0.0.0-#.#.#.#' ('{fullPath}', binding redirect for '{assemblyName}')");
            }

            var newVersionInAttribute = ParseAndValidatePreviousVersion(newVersion, newVersionAttribute?.Value, fullPath, "@newVersion", assemblyName);
            if (oldVersionInAttributeHigh != newVersionInAttribute)
            {
                throw new InvalidDataException($"The value of @newVersion should be equal to the end of the @oldVersion range ('{fullPath}', binding redirect for '{assemblyName}')");
            }

            var newVersionStr = newVersion.ToFullVersion().ToString();

            newVersionAttribute.SetValue(newVersionStr);
            oldVersionAttribute.SetValue("0.0.0.0-" + newVersionStr);
        }

        private void UpdateVersionsFile(string assemblyName, Version newVersion)
        {
            var fullPath = _versionsXmlFullPath;
            string variableName;

            switch (assemblyName)
            {
                case "System.Collections.Immutable":
                    variableName = "immutablearray";
                    break;

                case "Roslyn":
                    variableName = "vsmlangsroslyn";
                    break;

                default:
                    variableName = assemblyName.Replace(".", string.Empty) + "Version";
                    break;
            }

            var attributes = (from element in _versionsXml?.Root?.Element("versions")?.Elements("version")
                              where element.Attribute("name").Value == variableName
                              select element.Attribute("value"))?.ToArray();

            if (attributes == null || attributes.Length != 1)
            {
                throw new InvalidDataException($"Missing version element with @name='{variableName}' (file '{fullPath}')");
            }

            var valueAttribute = attributes.Single();

            ParseAndValidatePreviousVersion(newVersion, valueAttribute.Value, fullPath, "version.@name", assemblyName);

            valueAttribute.SetValue(newVersion.ToFullVersion());
        }

        private void UpdateAssemblyVersionsFile(string assemblyName, Version newVersion)
        {
            var fullPath = _versionsTemplateFullPath;
            var content = _versionsTemplateContent;

            string variableName;

            switch (assemblyName)
            {
                case "Roslyn":
                    variableName = "MicrosoftCodeAnalysisVersion";
                    break;

                default:
                    variableName = assemblyName.Replace(".", string.Empty) + "Version";
                    break;
            }

            var i = content.IndexOf(variableName);
            if (i < 0)
            {
                throw new InvalidDataException($"Definition of {variableName} not found in '{fullPath}'.");
            }

            var versionStart = content.IndexOf('"', i + variableName.Length) + 1;
            var versionEnd = content.IndexOf('"', versionStart);
            if (versionStart <= 0 || versionEnd < 0)
            {
                throw new InvalidDataException($"File '{fullPath}' doesn't have expected format.");
            }

            var versionStr = content.Substring(versionStart, versionEnd - versionStart);

            ParseAndValidatePreviousVersion(newVersion, versionStr, fullPath, variableName, assemblyName);

            _versionsTemplateContent = content.Substring(0, versionStart) + newVersion.ToFullVersion() + content.Substring(versionEnd);
        }

        private static bool TryParseVersionRange(string str, out Version low, out Version high)
        {
            low = high = null;

            var parts = str?.Split('-') ?? Array.Empty<string>();
            return parts.Length == 2 && Version.TryParse(parts[0], out low) && Version.TryParse(parts[1], out high);
        }

        private Version ParseAndValidatePreviousVersion(Version newVersion, string versionStringOpt, string fullPath, string description, string assemblyName)
        {
            // first time we run new insertion tool we need to skip checking previous version since it has a different format
            if (!Version.TryParse(versionStringOpt, out var previousVersion))
            {
                throw new InvalidDataException($"The value of {description} '{versionStringOpt}' doesn't have the expected format: '#.#.#.#' ('{fullPath}', binding redirect for '{assemblyName}')");
            }

            if (newVersion < previousVersion)
            {
                var warnMsg = $"Found binding redirect to a newer version than inserting, in file '{fullPath}' for assembly '{assemblyName}'." +
                    $"Inserting version {newVersion}, found {previousVersion}.";

                Log.Warn(warnMsg);
                WarningMessages.Add(warnMsg);
            }

            return previousVersion;
        }
    }
}
