// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.IO.Compression;

namespace RoslynTools
{
    // TODO: ZipDirectory is available on msbuild 15.8, remove this implementation once we update. 
    public sealed class ZipDirectory : Task
    {
        /// <summary>
        /// Gets or sets a <see cref="ITaskItem"/> containing the full path to the destination file to create.
        /// </summary>
        [Required]
        public ITaskItem DestinationFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the destination file should be overwritten.
        /// </summary>
        public bool Overwrite { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="ITaskItem"/> containing the full path to the source directory to create a zip archive from.
        /// </summary>
        [Required]
        public ITaskItem SourceDirectory { get; set; }

        public override bool Execute()
        {
            DirectoryInfo sourceDirectory = new DirectoryInfo(SourceDirectory.ItemSpec);

            if (!sourceDirectory.Exists)
            {
                Log.LogError($"Directory doesn't exist: {sourceDirectory.FullName}");
                return false;
            }

            FileInfo destinationFile = new FileInfo(DestinationFile.ItemSpec);

            BuildEngine3.Yield();

            try
            {
                if (destinationFile.Exists)
                {
                    if (!Overwrite)
                    {
                        Log.LogError($"File exists: {destinationFile.FullName}");
                        return false;
                    }

                    try
                    {
                        File.Delete(destinationFile.FullName);
                    }
                    catch (Exception e)
                    {
                        Log.LogErrorFromException(e);
                        return false;
                    }
                }

                try
                {
                    ZipFile.CreateFromDirectory(sourceDirectory.FullName, destinationFile.FullName);
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                }
            }
            finally
            {
                BuildEngine3.Reacquire();
            }

            return !Log.HasLoggedErrors;
        }
    }
}
