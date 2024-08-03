// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace Perf;

/// <summary>
/// This toolchain is designed to take an existing managed application
/// and run it in an external process.
/// </summary>
internal sealed class ExternalProcessToolchain(string exePath) : IToolchain
{
    public string Name => throw new System.NotImplementedException();

    public IGenerator Generator { get; } = new ExternalProcessGenerator(exePath);

    public IBuilder Builder { get; } = new ExternalProcessBuilder();

    public IExecutor Executor { get; } = new ExternalProcessExecutor();

    public bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        => benchmark is ExternalProcessBenchmark;
}
