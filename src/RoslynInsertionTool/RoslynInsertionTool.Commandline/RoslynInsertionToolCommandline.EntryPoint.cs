// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using static System.Threading.Tasks.SingleThreadExecutor;

partial class RoslynInsertionToolCommandline
{
    private static void Main(string[] args)
    {
        using (var cancellationTokenSource = new CancellationTokenSource())
        {
            Console.CancelKeyPress += (s, o) =>
            {
                cancellationTokenSource.Cancel();
                o.Cancel = true;
            };

            ExecuteTask(MainAsync(args, cancellationTokenSource.Token));
        }
    }
}