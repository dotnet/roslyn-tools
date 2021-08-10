// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using static System.Threading.Tasks.SingleThreadExecutor;

partial class RoslynInsertionToolCommandline
{
    private static async Task<int> Main(string[] args)
    {
        using (var cancellationTokenSource = new CancellationTokenSource())
        {
            Console.CancelKeyPress += (s, o) =>
            {
                cancellationTokenSource.Cancel();
                o.Cancel = true;
            };

            var passed = await MainAsync(args, cancellationTokenSource.Token);
            return passed ? 0 : -1;
        }
    }
}
