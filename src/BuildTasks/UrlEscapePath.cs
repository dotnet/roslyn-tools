// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RoslynTools
{
    public sealed class UrlEscapePath : Task
    {
        private static readonly char[] s_directorySeparators = new[] { '/', '\\' };

        [Required]
        public string LocalPath { get; set; }

        [Output]
        public string Url { get; private set; }

        public override bool Execute()
        {
            if (LocalPath == null)
            {
                return false;
            }

            Url = string.Join("/", LocalPath.Split(s_directorySeparators).Select(Uri.EscapeDataString));
            return true;
        }
    }
}
