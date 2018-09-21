// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static async Task<bool> CanBuildPartitionAsync(string relativePathToPartition, CancellationToken cancellationToken)
        {
            var result = await AsyncProcess.StartAsync(
                executable: @"C:\Windows\SysWOW64\cmd.exe",
                arguments: $@"/k cd /d ""{Options.EnlistmentPath}"" && src\tools\razzle.cmd ret && init.cmd && cd /d ""{Path.Combine(Options.EnlistmentPath, relativePathToPartition)}"" && msbuild /v:m /m dirs.proj",
                lowPriority: false,
                captureOutput: true,
                cancellationToken: cancellationToken);
            if (result.ExitCode != 0 || result.ErrorLines.Any())
            {
                Console.WriteLine($"Build exited with code {result.ExitCode}");
                Console.WriteLine($"Output:{Environment.NewLine}{string.Join(Environment.NewLine, result.OutputLines)}");
                Console.WriteLine($"Errors:{Environment.NewLine}{string.Join(Environment.NewLine, result.ErrorLines)}");
                return false;
            }

            Console.WriteLine($"Build of {relativePathToPartition} output:{Environment.NewLine}{string.Join(Environment.NewLine, result.OutputLines)}");
            return true;
        }
    }
}
