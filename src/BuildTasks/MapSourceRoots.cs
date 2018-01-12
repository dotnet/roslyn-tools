// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RoslynTools.BuildTasks
{
    // TODO: move to Microsoft.Build.Tasks
    public sealed class MapSourceRoots : Task
    {
        [Required]
        public ITaskItem[] SourceRoots { get; set; }

        [Output]
        public ITaskItem[] MappedSourceRoots { get; set; }

        public override bool Execute()
        {
            var topLevelMappedPaths = new Dictionary<string, string>();
            bool success = true;
            int i = 0;

            void SetTopLevelMappedPaths(bool sourceControl)
            {
                foreach (var root in SourceRoots)
                {
                    if (!string.IsNullOrEmpty(root.GetMetadata("SourceControl")) == sourceControl)
                    {
                        string nestedRoot = root.GetMetadata("NestedRoot");
                        if (string.IsNullOrEmpty(nestedRoot))
                        {
                            if (topLevelMappedPaths.ContainsKey(root.ItemSpec))
                            {
                                Log.LogError($"SourceRoot contains a duplicate: '{root.ItemSpec}'");
                                success = false;
                            }
                            else
                            {
                                var mappedPath = "/_" + (i == 0 ? "" : i.ToString()) + "/";
                                topLevelMappedPaths.Add(root.ItemSpec, mappedPath);
                                root.SetMetadata("MappedPath", mappedPath);
                                i++;
                            }
                        }
                    }
                }

            }

            // assign mapped paths to process source control roots first:
            SetTopLevelMappedPaths(sourceControl: true);

            // then assign mapped paths to other source control roots:
            SetTopLevelMappedPaths(sourceControl: false);

            // finally, calculate mapped paths of nested roots:
            foreach (var root in SourceRoots)
            {
                string nestedRoot = root.GetMetadata("NestedRoot");
                if (!string.IsNullOrEmpty(nestedRoot))
                {
                    string containingRoot = root.GetMetadata("ContainingRoot");
                    if (containingRoot != null && topLevelMappedPaths.TryGetValue(containingRoot, out var mappedTopLevelPath))
                    {
                        root.SetMetadata("MappedPath", Path.Combine(mappedTopLevelPath, nestedRoot.Replace('\\', '/')).EndWithSeparator('/'));
                    }
                    else
                    {
                        Log.LogError($"SourceRoot.ContainingRoot not found in SourceRoot items: '{containingRoot}'");
                        success = false;
                    }
                }
            }

            if (success)
            {
                MappedSourceRoots = SourceRoots;
            }

            return success;
        }
    }
}
