// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml.Linq;
using NuGet.Versioning;

namespace Roslyn.Tools.NuGet.Repack
{
    internal static class VersionUpdater
    {
        private const string DefaultNuspecXmlns = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd";

        private sealed class PackageInfo
        {
            public Package Package { get; }
            public string Id { get; }
            public string TempPathOpt { get; }
            public SemanticVersion OldVersion { get; }
            public SemanticVersion NewVersion { get; }

            public Stream SpecificationStream { get; }
            public XDocument SpecificationXml { get; }
            public string NuspecXmlns { get; }

            public PackageInfo(
                Package package,
                string id,
                SemanticVersion oldVersion,
                SemanticVersion newVersion,
                string tempPathOpt,
                Stream specificationStream,
                XDocument specificationXml,
                string nuspecXmlns)
            {
                SpecificationStream = specificationStream;
                SpecificationXml = specificationXml;
                Package = package;
                Id = id;
                TempPathOpt = tempPathOpt;
                OldVersion = oldVersion;
                NewVersion = newVersion;
                NuspecXmlns = nuspecXmlns;
            }
        }

        public static void Run(IEnumerable<string> packagePaths, string outDirectoryOpt, bool release)
        {
            string tempDirectoryOpt;
            if (outDirectoryOpt != null)
            {
                tempDirectoryOpt = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectoryOpt);
            }
            else
            {
                tempDirectoryOpt = null;
            }

            var packages = new Dictionary<string, PackageInfo>();
            try
            {
                LoadPackages(packagePaths, packages, tempDirectoryOpt, release);
                UpdateDependencies(packages, release);

                if (outDirectoryOpt != null)
                {
                    SavePackages(packages, outDirectoryOpt);
                }
            }
            finally
            {
                foreach (var package in packages.Values)
                {
                    package.SpecificationStream.Dispose();
                    package.Package.Close();
                }

                if (tempDirectoryOpt != null)
                {
                    Directory.Delete(tempDirectoryOpt, recursive: true);
                }
            }
        }

        private static void LoadPackages(IEnumerable<string> packagePaths, Dictionary<string, PackageInfo> packages, string tempDirectoryOpt, bool release)
        {
            bool readOnly = tempDirectoryOpt == null;

            foreach (var packagePath in packagePaths)
            {
                Package package;
                string tempPathOpt;
                if (readOnly)
                {
                    tempPathOpt = null;
                    package = Package.Open(packagePath, FileMode.Open, FileAccess.Read);
                }
                else
                {
                    tempPathOpt = Path.Combine(tempDirectoryOpt, Guid.NewGuid().ToString());
                    File.Copy(packagePath, tempPathOpt);
                    package = Package.Open(tempPathOpt, FileMode.Open, FileAccess.ReadWrite);
                }

                string packageId = null;
                Stream nuspecStream = null;
                XDocument nuspecXml = null;

                PackageInfo packageInfo = null;
                try
                {
                    SemanticVersion packageVersion = null;
                    SemanticVersion newPackageVersion = null;
                    string nuspecXmlns = DefaultNuspecXmlns;

                    foreach (var part in package.GetParts())
                    {
                        string relativePath = part.Uri.OriginalString;
                        ParsePartName(relativePath, out var fileName, out var dirName);

                        if (dirName == "/" && fileName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                        {
                            nuspecStream = part.GetStream(FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
                            nuspecXml = XDocument.Load(nuspecStream);

                            if (nuspecXml.Root.HasAttributes)
                            {
                                var xmlNsAttribute = nuspecXml.Root.Attributes("xmlns").SingleOrDefault();
                                if (xmlNsAttribute != null)
                                {
                                    nuspecXmlns = xmlNsAttribute.Value;
                                }
                            }
                            var metadata = nuspecXml.Element(XName.Get("package", nuspecXmlns))?.Element(XName.Get("metadata", nuspecXmlns));
                            if (metadata == null)
                            {
                                throw new InvalidDataException($"'{packagePath}' has invalid nuspec: missing 'metadata' element");
                            }

                            packageId = metadata.Element(XName.Get("id", nuspecXmlns))?.Value;
                            if (packageId == null)
                            {
                                throw new InvalidDataException($"'{packagePath}' has invalid nuspec: missing 'id' element");
                            }

                            var versionElement = metadata.Element(XName.Get("version", nuspecXmlns));
                            string packageVersionStr = versionElement?.Value;
                            if (packageVersionStr == null)
                            {
                                throw new InvalidDataException($"'{packagePath}' has invalid nuspec: missing 'version' element");
                            }

                            if (!SemanticVersion.TryParse(packageVersionStr, out packageVersion))
                            {
                                throw new InvalidDataException($"'{packagePath}' has invalid nuspec: invalid 'version' value '{packageVersionStr}'");
                            }

                            if (!packageVersion.IsPrerelease)
                            {
                                throw new InvalidOperationException($"Can only update pre-release packages: '{packagePath}' has release version");
                            }

                            // To strip build number take the first part of the pre-release label (e.g. "beta1-62030-10")
                            string releaseLabel = release ? null : packageVersion.Release.Split('-').First();

                            newPackageVersion = new SemanticVersion(packageVersion.Major, packageVersion.Minor, packageVersion.Patch, releaseLabel);

                            if (!readOnly)
                            {
                                versionElement.SetValue(newPackageVersion.ToNormalizedString());
                            }

                            break;
                        }
                    }

                    if (nuspecStream == null)
                    {
                        throw new InvalidDataException($"'{packagePath}' is missing .nuspec file");
                    }

                    if (packages.ContainsKey(packageId))
                    {
                        throw new InvalidDataException($"Multiple packages of name '{packageId}' specified");
                    }

                    if (!readOnly)
                    {
                        package.PackageProperties.Version = newPackageVersion.ToNormalizedString();
                    }
                    
                    packageInfo = new PackageInfo(package, packageId, packageVersion, newPackageVersion, tempPathOpt, nuspecStream, nuspecXml, nuspecXmlns);
                }
                finally
                {
                    if (packageInfo == null)
                    {
                        nuspecStream.Dispose();
                        package.Close();

                        if (tempPathOpt != null)
                        {
                            File.Delete(tempPathOpt);
                        }
                    }
                }

                packages.Add(packageId, packageInfo);
            }
        }

        private static void ParsePartName(string relativePath, out string fileName, out string dirName)
        {
            int lastSeparator = relativePath.LastIndexOf('/');
            fileName = relativePath.Substring(lastSeparator + 1);
            dirName = (lastSeparator == -1) ? "" : (lastSeparator == 0) ? "/" : relativePath.Substring(0, lastSeparator);
        }

        private static void UpdateDependencies(Dictionary<string, PackageInfo> packages, bool release)
        {
            var errors = new List<Exception>();

            foreach (var package in packages.Values)
            {
                var dependencies = package.SpecificationXml.
                    Element(XName.Get("package", package.NuspecXmlns))?.
                    Element(XName.Get("metadata", package.NuspecXmlns))?.
                    Element(XName.Get("dependencies", package.NuspecXmlns))?.
                    Descendants(XName.Get("dependency", package.NuspecXmlns)) ?? Array.Empty<XElement>();

                foreach (var dependency in dependencies)
                {
                    string id = dependency.Attribute("id")?.Value;
                    if (id == null)
                    {
                        throw new InvalidDataException($"'{package.Id}' has invalid format: element 'dependency' is missing 'id' attribute");
                    }

                    var versionRangeAttribute = dependency.Attribute("version");
                    if (versionRangeAttribute == null)
                    {
                        throw new InvalidDataException($"'{package.Id}' has invalid format: element 'dependency' is missing 'version' attribute");
                    }

                    if (!VersionRange.TryParse(versionRangeAttribute.Value, out var versionRange))
                    {
                        throw new InvalidDataException($"'{id}' has invalid version range: '{versionRangeAttribute.Value}'");
                    }

                    if (packages.TryGetValue(id, out var dependentPackage))
                    {
                        if (versionRange.IsFloating ||
                            versionRange.HasLowerAndUpperBounds && versionRange.MinVersion != versionRange.MaxVersion)
                        {
                            throw new InvalidDataException($"Unexpected dependency version range: '{id}, {versionRangeAttribute.Value}'");
                        }

                        var newVersion = ToNuGetVersion(dependentPackage.NewVersion);

                        var newRange = new VersionRange(
                            versionRange.HasLowerBound ? newVersion : null,
                            versionRange.IsMinInclusive,
                            versionRange.HasUpperBound ? newVersion : null,
                            versionRange.IsMaxInclusive);

                        versionRangeAttribute.SetValue(newRange.ToNormalizedString());
                    }
                    else if (release && (versionRange.MinVersion?.IsPrerelease == true || versionRange.MaxVersion?.IsPrerelease == true))
                    {
                        errors.Add(new InvalidOperationException($"Package '{package.Id}' depends on a pre-release package '{id}, {versionRangeAttribute.Value}'"));
                    }
                }
            }

            ThrowExceptions(errors);
        }

        private static void SavePackages(Dictionary<string, PackageInfo> packages, string outDirectory)
        {
            Directory.CreateDirectory(outDirectory);

            var errors = new List<Exception>();
            foreach (var package in packages.Values)
            {
                package.SpecificationStream.SetLength(0);
                package.SpecificationXml.Save(package.SpecificationStream);

                package.Package.Close();
                
                string finalPath = Path.Combine(outDirectory, package.Id + "." + package.NewVersion + ".nupkg");

                try
                {
                    File.Copy(package.TempPathOpt, finalPath, overwrite: true);
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

            ThrowExceptions(errors);
        }

        private static void ThrowExceptions(IReadOnlyCollection<Exception> exceptions)
        {
            if (exceptions.Count == 1)
            {
                throw exceptions.Single();
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions.ToArray());
            }
        }

        private static NuGetVersion ToNuGetVersion(SemanticVersion version)
            => new NuGetVersion(version.Major, version.Minor, version.Patch, version.ReleaseLabels, version.Metadata);
    }
}
