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
            string capName;

            switch (assemblyName)
            {
                case "System.Collections.Immutable":
                    variableName = "immutablearray";
                    capName = "IMMUTABLEARRAY_VERSION";
                    break;

                case "Roslyn":
                    variableName = "vsmlangsroslyn";
                    capName = "VSMLANGSROSLYN_VERSION";
                    break;

                default:
                    variableName = assemblyName.Replace(".", string.Empty) + "Version";
                    capName = assemblyName.Replace(".", "_").ToUpperInvariant() + "_VERSION";
                    break;
            }

            var versionsElement = _versionsXml?.Root?.Element("versions");
            var sourceElements = versionsElement?.Elements("version");
            if (sourceElements == null)
            {
                throw new InvalidDataException($"Invalid XML data structure.  Expected: /root/versions/version (file '{fullPath}')");
            }

            var attributes = (from element in sourceElements
                              where element.Attribute("name")?.Value == variableName
                              select element.Attribute("value")).ToArray();

            if (attributes.Length == 0)
            {
                var newVersionElement = new XElement("version",
                    new XAttribute("name", variableName),
                    new XAttribute("value", newVersion.ToFullVersion()),
                    new XAttribute("CAPNAME", capName));
                versionsElement.Add(newVersionElement);
            }
            else if (attributes.Length == 1)
            {
                var valueAttribute = attributes.Single();
                ParseAndValidatePreviousVersion(newVersion, valueAttribute.Value, fullPath, "version.@name", assemblyName);
                valueAttribute.SetValue(newVersion.ToFullVersion());
            }
            else
            {
                throw new InvalidDataException($"More than one element with @name='{variableName}' (file '{fullPath}')");
            }
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

            // the structure of the file is:
            //   header: opening tag:    <#+
            //           comments:           //
            //   values:                     const string AssemblyNameVersion = "<version-number>";
            //   footer: closing tag:    #>
            var lines = content.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
            var finalLines = new List<string>();
            bool IsVersionLine(string line) => line.TrimStart().StartsWith("const");
            var headerLines = lines.TakeWhile(line => !IsVersionLine(line)).ToList();
            var valueLines = lines.Skip(headerLines.Count).TakeWhile(IsVersionLine).ToList();
            var footerLines = lines.Skip(headerLines.Count + valueLines.Count).ToList();

            // find the variable or the appropriate insertion location
            var constExpression = "const string";
            string GetLineVariableName(string line)
            {
                var startIndex = line.IndexOf(constExpression) + constExpression.Length + 1;
                var endIndex = line.IndexOf(' ', startIndex + 1);
                var variableLength = endIndex - startIndex;
                return line.Substring(startIndex, variableLength);
            }
            var sortedValueLines = valueLines.OrderBy(GetLineVariableName).ToList();

            // rather than get fancy, linearly search through the sorted list and update or add as appropriate
            // the file is small so this is fine
            bool valueUpdated = false;
            var newLine = $@"    const string {variableName} = ""{newVersion.ToFullVersion()}"";";
            for (int lineIndex = 0; lineIndex < sortedValueLines.Count; lineIndex++)
            {
                var line = sortedValueLines[lineIndex];
                var currentVariableName = GetLineVariableName(line);
                var comparison = String.Compare(currentVariableName, variableName, StringComparison.OrdinalIgnoreCase);
                if (comparison == 0)
                {
                    // found exact match, replace this line
                    var versionStart = IndexOfOrThrow(line, '"') + 1;
                    var versionEnd = IndexOfOrThrow(line, '"', versionStart);
                    var versionStr = line.Substring(versionStart, versionEnd - versionStart);
                    ParseAndValidatePreviousVersion(newVersion, versionStr, fullPath, variableName, assemblyName);
                    newLine = line.Substring(0, versionStart) + newVersion.ToFullVersion() + line.Substring(versionEnd);
                    sortedValueLines[lineIndex] = newLine;
                    valueUpdated = true;
                    break;
                }
                else if (comparison > 0)
                {
                    // we passed it, add it right before this line
                    sortedValueLines.Insert(lineIndex, newLine);
                    valueUpdated = true;
                    break;
                }
            }

            if (!valueUpdated)
            {
                // value was last alphabetically, just add it to the end
                sortedValueLines.Add(newLine);
            }

            var allLines = headerLines.Concat(sortedValueLines).Concat(footerLines);
            _versionsTemplateContent = string.Join("\r\n", allLines);
        }

        private static int IndexOfOrThrow(string str, char value, int startIndex = 0)
        {
            var result = str.IndexOf(value, startIndex);
            if (result < 0)
            {
                throw new InvalidDataException($"The specified character '{value}' was not found in the string: {str}");
            }

            return result;
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
