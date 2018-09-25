// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RoslynTools
{
    public class GetAssemblyFullName : Task
    {
        [Required]
        public ITaskItem[] Items { get; set; }

        [Required]
        public string PathMetadata { get; set; }

        [Required]
        public string FullNameMetadata { get; set; }

        [Output]
        public ITaskItem[] ItemsWithFullName { get; set; }

        public override bool Execute()
        {
            ItemsWithFullName = Items;

            foreach (var item in Items)
            {
                item.SetMetadata(FullNameMetadata, AssemblyName.GetAssemblyName(item.GetMetadata(PathMetadata)).FullName);
            }

            return true;
        }
    }
}

