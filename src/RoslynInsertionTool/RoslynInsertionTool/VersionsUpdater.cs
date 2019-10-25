// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.TeamFoundation.Client.CommandLine;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Roslyn.Insertion
{
    internal sealed class VersionsUpdater
    {
        public List<string> WarningMessages { get; }

        private const string ConfigPath = "src/VSSDK/VSIntegration/IsoShell/Templates/VSShellTemplate/VSShellIso/VSShellStubExe/Stub.exe.config";
        private readonly XDocument _configXml;

        private const string VersionsPath = "src/ProductData/versions.xml";
        private readonly XDocument _versionsXml;

        private const string VersionsTemplatePath = "src/ProductData/AssemblyVersions.tt";
        private string _versionsTemplateContent;

        public VersionsUpdater(GitHttpClient gitClient, string commitId, List<string> warningMessages)
        {
            WarningMessages = warningMessages;

            var vsRepoId = RoslynInsertionTool.VSRepoId;
            var version = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };

            // TODO: consider refactoring into a CreateAsync or similar method to avoid .Result
            var configXmlContent = gitClient.GetItemContentAsync(vsRepoId, ConfigPath, download: true, versionDescriptor: version).Result;
            _configXml = XDocument.Load(configXmlContent);

            var versionsXmlContent = gitClient.GetItemContentAsync(vsRepoId, VersionsPath, download: true, versionDescriptor: version).Result;
            _versionsXml = XDocument.Load(versionsXmlContent);

            // template defining version variables that flow to .config.tt files:
            var versionsTemplateContent = gitClient.GetItemContentAsync(vsRepoId, VersionsTemplatePath, download: true, versionDescriptor: version).Result;
            using (StreamReader reader = new StreamReader(versionsTemplateContent))
            {
                _versionsTemplateContent = reader.ReadToEnd();
            }
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

        public GitChange[] GetChanges()
        {
            var changes = new[]
            {
                new GitChange
                {
                    ChangeType = VersionControlChangeType.Edit,
                    Item = new GitItem { Path = ConfigPath },
                    NewContent = new ItemContent() { Content = _configXml.ToFullString(), ContentType = ItemContentType.RawText }
                },
                new GitChange
                {
                    ChangeType = VersionControlChangeType.Edit,
                    Item = new GitItem { Path = VersionsPath },
                    NewContent = new ItemContent() { Content = _versionsXml.ToFullString(), ContentType = ItemContentType.RawText }
                },
                new GitChange
                {
                    ChangeType = VersionControlChangeType.Edit,
                    Item = new GitItem { Path = VersionsTemplatePath },
                    NewContent = new ItemContent() { Content = _versionsTemplateContent, ContentType = ItemContentType.RawText }
                }
            };

            return changes;
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

            var dependentAssemblies = _configXml?.Root?.
                Element("runtime")?.
                Element(XName.Get("assemblyBinding", ns))?.
                Elements(XName.Get("dependentAssembly", ns));

            if (dependentAssemblies == null)
            {
                throw new InvalidDataException($"File '{ConfigPath}' doesn't have expected format.");
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
                throw new InvalidDataException($"The value of attribute oldVersion '{oldVersionAttribute?.Value}' doesn't have the expected format: '0.0.0.0-#.#.#.#' ('{ConfigPath}', binding redirect for '{assemblyName}')");
            }

            var newVersionInAttribute = ParseAndValidatePreviousVersion(newVersion, newVersionAttribute?.Value, ConfigPath, "@newVersion", assemblyName);
            if (oldVersionInAttributeHigh != newVersionInAttribute)
            {
                throw new InvalidDataException($"The value of @newVersion should be equal to the end of the @oldVersion range ('{ConfigPath}', binding redirect for '{assemblyName}')");
            }

            var newVersionStr = newVersion.ToFullVersion().ToString();

            newVersionAttribute.SetValue(newVersionStr);
            oldVersionAttribute.SetValue("0.0.0.0-" + newVersionStr);
        }

        private void UpdateVersionsFile(string assemblyName, Version newVersion)
        {
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
                throw new InvalidDataException($"Invalid XML data structure.  Expected: /root/versions/version (file '{VersionsPath}')");
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
                var oldVersion = ParseAndValidatePreviousVersion(newVersion, valueAttribute.Value, VersionsPath, "version.@name", assemblyName);
                if (newVersion > oldVersion)
                {
                    valueAttribute.SetValue(newVersion.ToFullVersion());
                }
            }
            else
            {
                throw new InvalidDataException($"More than one element with @name='{variableName}' (file '{VersionsPath}')");
            }
        }

        private void UpdateAssemblyVersionsFile(string assemblyName, Version newVersion)
        {
            var path = VersionsTemplatePath;
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
                    var oldVersion = ParseAndValidatePreviousVersion(newVersion, versionStr, path, variableName, assemblyName);
                    if (newVersion > oldVersion)
                    {
                        newLine = line.Substring(0, versionStart) + newVersion.ToFullVersion() + line.Substring(versionEnd);
                        sortedValueLines[lineIndex] = newLine;
                    }
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

        private Version ParseAndValidatePreviousVersion(Version newVersion, string versionStringOpt, string path, string description, string assemblyName)
        {
            // first time we run new insertion tool we need to skip checking previous version since it has a different format
            if (!Version.TryParse(versionStringOpt, out var previousVersion))
            {
                throw new InvalidDataException($"The value of {description} '{versionStringOpt}' doesn't have the expected format: '#.#.#.#' ('{path}', binding redirect for '{assemblyName}')");
            }

            return previousVersion;
        }
    }
}
