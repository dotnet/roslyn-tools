﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignTool
{
    internal struct ZipPart
    {
        internal string RelativeName { get; }
        internal FileName FileName { get; }
        internal string Checksum { get; }

        internal ZipPart(string relativeName, FileName fileName, string checksum)
        {
            RelativeName = relativeName;
            FileName = fileName;
            Checksum = checksum;
        }

        public override string ToString() => $"{RelativeName} -> {FileName.RelativePath} -> {Checksum}";
    }
}


