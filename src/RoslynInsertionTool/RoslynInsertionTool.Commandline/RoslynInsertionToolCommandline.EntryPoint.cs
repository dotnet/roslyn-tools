// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

partial class RoslynInsertionToolCommandline
{
    private static async Task<int> Main(string[] args)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (s, o) =>
        {
            cancellationTokenSource.Cancel();
            o.Cancel = true;
        };

        var passed = await MainAsync(args, cancellationTokenSource.Token);
        return passed ? 0 : -1;
    }
}
