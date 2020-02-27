// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.TeamFoundation.Client.CommandLine;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Roslyn.Insertion
{
    internal sealed class VersionsUpdater
    {
        private const string VersionsTemplatePath = "src/ProductData/AssemblyVersions.tt";
        private readonly string _versionsTemplateOriginal;
        private string _versionsTemplateContent;

        public List<string> WarningMessages { get; }

        private VersionsUpdater(string versionsTemplate, List<string> warningMessages)
        {
            _versionsTemplateOriginal = versionsTemplate;
            _versionsTemplateContent = versionsTemplate;
            WarningMessages = warningMessages;
        }

        public static async Task<VersionsUpdater> Create(GitHttpClient gitClient, string commitId, List<string> warningMessages)
        {
            var vsRepoId = RoslynInsertionTool.VSRepoId;
            var version = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };
            var versionsTemplateContent = await gitClient.GetItemContentAsync(vsRepoId, VersionsTemplatePath, download: true, versionDescriptor: version);
            using var reader = new StreamReader(versionsTemplateContent);
            string versionsTemplate = reader.ReadToEnd();
            return new VersionsUpdater(versionsTemplate, warningMessages);
        }

        public GitChange GetChangeOpt()
        {
            return RoslynInsertionTool.GetChangeOpt(VersionsTemplatePath, _versionsTemplateOriginal, _versionsTemplateContent);
        }

        public void UpdateComponentVersion(string assemblyName, Version newVersion)
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
