// Licensed to the .NET Foundation under one or more agreements.
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace Roslyn.Insertion
{
    internal class CoreXT
    {
        private static Dictionary<string, string> ComponentToFileMap = null!;
        private static Dictionary<string, (string original, JObject document)> ComponentFileToDocumentMap = null!;
        private static HashSet<string> dirtyComponentFiles = null!;

        private static Dictionary<string, ICollection<string>> PackageToPropFilesMap = null!;
        private static Dictionary<string, (string original, XDocument document)> PackagePropFileToDocumentMap = null!;
        private static HashSet<string> dirtyPropsFiles = null!;

        private const string DefaultConfigPath = ".corext/Configs/default.config";
        private const string ComponentsJsonPath = ".corext/Configs/components.json";
        private const string PackagePropsPath = "Directory.Packages.props";
        private const string LegacyPackagePropsPath = "Packages.props";

        private readonly string _defaultConfigOriginal;

        public XDocument ConfigDocument { get; }

        private CoreXT(string configOriginalText)
        {
            _defaultConfigOriginal = configOriginalText;
            ConfigDocument = XDocument.Parse(configOriginalText, LoadOptions.None);
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

            ComponentToFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ComponentFileToDocumentMap = new Dictionary<string, (string, JObject)>(StringComparer.OrdinalIgnoreCase);
            dirtyComponentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            PackageToPropFilesMap = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);
            PackagePropFileToDocumentMap = new Dictionary<string, (string original, XDocument document)>(StringComparer.OrdinalIgnoreCase);
            dirtyPropsFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await PopulateComponentJsonMaps(gitClient, commitId);
            await PopulatePackagePropFileMaps(gitClient, commitId);

            return new CoreXT(defaultConfigOriginal);
        }

        public IEnumerable<GitChange?> SaveConfigs()
        {
            yield return RoslynInsertionTool.GetChangeOpt(DefaultConfigPath, _defaultConfigOriginal, toFullString(_defaultConfigOriginal, ConfigDocument));

            foreach (var propFile in dirtyPropsFiles)
            {
                (string? original, XDocument? document) pair = (null, null);
                if (PackagePropFileToDocumentMap.TryGetValue(propFile, out pair!))
                {
                    yield return RoslynInsertionTool.GetChangeOpt(propFile, pair.original, toFullString(pair.original, pair.document));
                }
            }

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
                if (dirtyComponentFiles.Contains(kvp.Key))
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

        private XAttribute? GetDefaultConfigVersionAttribute(PackageInfo packageInfo)
        {
            return ConfigDocument.Root
                .Elements("packages")
                .Elements("package")
                .Where(p => p.Attribute("id")?.Value == packageInfo.PackageName)
                .Select(x => x.Attribute("version")).SingleOrDefault();
        }

        private static XAttribute? GetVersionAttributeInPropsFileOpt(XDocument document, PackageInfo packageInfo)
        {
            // Support both the existing tags - "PackageVersion" and "Include" as well the legacy tags "PackageReference" and "Update" so we could still update servicing branches.
            return document?.Root
                .Elements().Where(e => string.Equals(e.Name.LocalName, "ItemGroup"))
                .Elements().Where(e => string.Equals(e.Name.LocalName, "PackageVersion") || string.Equals(e.Name.LocalName, "PackageReference"))
                .Where(p => p.Attribute("Include")?.Value == packageInfo.PackageName || p.Attribute("Update")?.Value == packageInfo.PackageName)
                .Select(x => x.Attribute("Version")).SingleOrDefault();
        }

        private XElement GetClosestFollowingPackageElement(PackageInfo packageInfo)
        {
            return ConfigDocument.Root.
                Elements("packages").
                Elements("package").
                FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Compare(packageInfo.PackageName, p.Attribute("id").Value) < 0);
        }

        internal void AddNewPackage(PackageInfo packageInfo)
        {
            Console.WriteLine("##vso[task.logissue type=warning] Adding a new package is not supported until we also update for LegacyProjects.props");

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

            if (PackageToPropFilesMap.TryGetValue(packageInfo.PackageName, out var propFiles))
            {
                foreach (var file in propFiles)
                {
                    (string? original, XDocument? document) pair = (null, null);
                    if (PackagePropFileToDocumentMap.TryGetValue(file, out pair!))
                    {
                        var propsVersionAttribute = GetVersionAttributeInPropsFileOpt(pair.document!, packageInfo);
                        propsVersionAttribute?.SetValue(packageInfo.Version.ToString());

                        dirtyPropsFiles.Add(file);
                    }
                }
            }
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
                dirtyComponentFiles.Add(componentFilePath);
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

        private static Task<bool> IsFilePresent(
            GitHttpClient gitClient,
            GitVersionDescriptor versionDescriptor,
            string filePath)
        {
            try
            {
                var packagePropsItem = await gitClient.GetItemsAsync(RoslynInsertionTool.VSRepoId,
                    scopePath: filePath,
                    recursionLevel: VersionControlRecursionType.Full,
                    download: true,
                    versionDescriptor: versionDescriptor);
                return packagePropsItem.Any();
            }
            catch
            {
                return false;
            }
        }

        private static async Task PopulatePackagePropFileMaps(
            GitHttpClient gitClient,
            string commitId)
        {
            var versionDescriptor = new GitVersionDescriptor { VersionType = GitVersionType.Commit, Version = commitId };

            var packagePropsFile = IsFilePresent(gitClient, versionDescriptor, PackagePropsPath)
                ? PackagePropsPath
                : LegacyPackagePropsPath;

            await ProcessPropsPath(packagePropsFile, versionDescriptor);

            if (PackagePropFileToDocumentMap.Any())
            {
                var parentPackagesDoc = PackagePropFileToDocumentMap.First().Value.document;

                var importedPropPaths = parentPackagesDoc.Root.Elements().Where(e => string.Equals(e.Name.LocalName, "Import"))
                    .Select(e => e.Attributes("Project").FirstOrDefault()?.Value);

                foreach(var propPath in importedPropPaths)
                {
                    if (propPath == null) continue;

                    if (propPath.EndsWith(@"\**\*.props"))
                    {
                        await ProcessPropsPath(propPath.Substring(0, propPath.IndexOf(@"\**\*.props")), versionDescriptor);
                    }
                    else if (propPath.EndsWith(@"\*.props"))
                    {
                        await ProcessPropsPath(propPath.Substring(0, propPath.IndexOf(@"\*.props")), versionDescriptor);
                    }
                    else
                    {
                        await ProcessPropsPath(propPath, versionDescriptor);
                    }
                }
            }

            async Task ProcessPropsPath(string propsPath, GitVersionDescriptor versionDescriptor)
            {
                var propsFiles = await gitClient.GetItemsAsync(RoslynInsertionTool.VSRepoId,
                    scopePath: propsPath,
                    recursionLevel: VersionControlRecursionType.Full,
                    download: true,
                    versionDescriptor: versionDescriptor);

                foreach (var item in propsFiles)
                {
                    if (item.IsFolder || item.IsSymbolicLink)
                    {
                        continue;
                    }

                    try
                    {
                        using var fileStream = await gitClient.GetItemContentAsync(RoslynInsertionTool.VSRepoId, path: item.Path, versionDescriptor: versionDescriptor);
                        var content = await new StreamReader(fileStream).ReadToEndAsync();
                        var (original, document) = (content, XDocument.Parse(content));
                        if (document != null && !PackagePropFileToDocumentMap.ContainsKey(item.Path))
                        {
                            PackagePropFileToDocumentMap[item.Path] = (original, document);
                        }

                        PopulatePackageToPropFileMap(document!, item.Path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to parse file {item.Path}");
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static void PopulatePackageToPropFileMap(XDocument document, string propFileName)
        {
            try
            {
                // "PackageReference" and "Update" tags are the legacy nomenclature that we still need to support for servicing branches.
                var packageRefs = document.Root
                    .Elements().Where(e => string.Equals(e.Name.LocalName, "ItemGroup"))
                    .Elements().Where(e => string.Equals(e.Name.LocalName, "PackageVersion") || string.Equals(e.Name.LocalName, "PackageReference"));

                foreach (var packageRef in packageRefs)
                {
                    var name = packageRef.Attribute("Include")?.Value;
                    if (string.IsNullOrEmpty(name))
                    {
                        // Try the legacy nomenclature
                        name = packageRef.Attribute("Update")?.Value;
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!PackageToPropFilesMap.ContainsKey(name!))
                        {
                            PackageToPropFilesMap[name!] = new List<string>();
                        }

                        PackageToPropFilesMap[name!].Add(propFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                // Swallow exceptions reading any of these files.
                Console.WriteLine($"Could not load contents of {propFileName}");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
