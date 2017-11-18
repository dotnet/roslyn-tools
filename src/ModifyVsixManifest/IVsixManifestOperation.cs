// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace ModifyVsixManifest
{
    internal interface IVsixManifestOperation
    {
        void Execute(XDocument document);
    }
}
