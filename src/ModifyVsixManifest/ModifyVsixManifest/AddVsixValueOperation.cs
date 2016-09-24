// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ModifyVsixManifest
{
    internal class AddVsixValueOperation : IVsixManifestOperation
    {
        public string Path { get; }
        public string AttributeName { get; }
        public string Value { get; }

        public AddVsixValueOperation(string path, string attributeName, string value)
        {
            Path = path;
            AttributeName = attributeName;
            Value = value;
        }

        public static AddVsixValueOperation FromSemicolonDelimited(string str)
        {
            var parts = str.Split(';');
            Debug.Assert(parts.Length == 3);
            return new AddVsixValueOperation(parts[0], parts[1], parts[2]);
        }

        public void Execute(XDocument document)
        {
            var navigator = document.CreateNavigator();
            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("x", document.Root.Name.NamespaceName);

            // unfortunately evaluating
            var enumerable = (IEnumerable<object>)document.XPathEvaluate(Path, namespaceManager);
            var node = enumerable.FirstOrDefault() as XObject;
            if (node is XElement)
            {
                var element = (XElement)node;
                element.Add(new XAttribute(AttributeName, Value));
            }
            else
            {
                throw new Exception("Unable to find element via XPath");
            }
        }
    }
}
