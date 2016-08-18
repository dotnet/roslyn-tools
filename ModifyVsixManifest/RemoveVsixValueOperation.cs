// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ModifyVsixManifest
{
    internal class RemoveVsixValueOperation : IVsixManifestOperation
    {
        public string Path { get; }

        public RemoveVsixValueOperation(string path)
        {
            Path = path;
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
                ((XElement)node).Remove();
            }
            else if (node is XAttribute)
            {
                var att = (XAttribute)node;
                var parent = att.Parent;
                var newAttributes = parent.Attributes().Except(new[] { att });
                parent.RemoveAttributes();
                parent.Add(newAttributes);
            }
            else
            {
                throw new Exception("Unable to find element via XPath.");
            }
        }
    }
}
