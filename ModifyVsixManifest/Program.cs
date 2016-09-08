// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Xml.Linq;
using Mono.Options;

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
                var part = package.GetPart(new Uri("/extension.vsixmanifest", UriKind.Relative));
                using (var stream = part.GetStream(FileMode.Open))
                {
                    var document = XDocument.Load(stream);
                    foreach (var operation in operations)
                    {
                        operation.Execute(document);
                    }

                    using (var ms = new MemoryStream())
                    {
                        document.Save(ms);
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.SetLength(ms.Length);
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.CopyTo(stream);
                    }
                }
            }
        }
    }
}
