// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Mono.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModifyVsixManifest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string vsixName = null;
            var operations = new List<IVsixManifestOperation>();
            var parameters = new OptionSet()
            {
                @"Usage: {exename} --vsix=path\to\package.vsix [options]",
                "",
                "Options:",
                { "vsix=", "The VSIX package to modify.", value => vsixName = value },
                { "add-attribute=", "The XPath of the parent to the attribute, the attribute name, and the value to add, all separated by semicolons.", value => operations.Add(AddVsixValueOperation.FromSemicolonDelimited(value)) },
                { "remove=", "The XPath of the value to remove.", path => operations.Add(new RemoveVsixValueOperation(path)) }
            };

            if (args.Length == 0)
            {
                parameters.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }

            try
            {
                parameters.Parse(args);
            }
            catch (OptionException)
            {
                parameters.WriteOptionDescriptions(Console.Out);
                Environment.Exit(1);
            }

            using (var package = Package.Open(vsixName))
            {
                var (name, hash) = UpdateExtensionVsixManifest(package, operations);
                UpdatePartHashInManifestJson(package, name, hash);
            }
        }

        private static (string name, byte[] hash) UpdateExtensionVsixManifest(Package package, List<IVsixManifestOperation> operations)
        {
            var partName = "/extension.vsixmanifest";
            var part = package.GetPart(new Uri(partName, UriKind.Relative));

            byte[] hash;
            using (var stream = part.GetStream(FileMode.Open))
            {
                var document = XDocument.Load(stream);
                foreach (var operation in operations)
                {
                    operation.Execute(document);
                }

                using (var newContent = new MemoryStream())
                {
                    document.Save(newContent);

                    // overwrite the content of the part in VSIX:
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(newContent.Length);
                    newContent.Seek(0, SeekOrigin.Begin);
                    newContent.CopyTo(stream);

                    // calculate new hash:
                    newContent.Seek(0, SeekOrigin.Begin);
                    using (var sha = SHA256.Create())
                    {
                        hash = sha.ComputeHash(newContent);
                    }
                }
            }

            return (partName, hash);
        }

        private static void UpdatePartHashInManifestJson(Package package, string partName, byte[] partHash)
        {
            var part = package.GetPart(new Uri("/manifest.json", UriKind.Relative));

            using (var stream = part.GetStream(FileMode.Open))
            {
                string jsonStr;
                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 2048, leaveOpen: true))
                {
                    jsonStr = reader.ReadToEnd();
                }

                var json = JObject.Parse(jsonStr);

                var file = ((JArray)json["files"]).Where(f => (string)f["fileName"] == partName).Single();
                file["sha256"] = BitConverter.ToString(partHash).Replace("-", "");

                stream.Position = 0;
                stream.SetLength(0);

                using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 2048, leaveOpen: false))
                {
                    writer.Write(json.ToString(Formatting.None));
                }
            }
        }
    }
}
