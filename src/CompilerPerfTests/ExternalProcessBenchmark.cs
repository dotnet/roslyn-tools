// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace Perf;

internal sealed class ExternalProcessBenchmark(
    string workingDir,
    string arguments,
    Func<string, int> buildFunc,
    Job job,
    ParameterInstances parameterInstances)
    : Benchmark(GetTarget(), job, parameterInstances)
{
    public string WorkingDirectory { get; } = workingDir;

    public string Arguments { get; } = arguments;

    public Func<string, int> BuildFunc { get; } = buildFunc;

    /// <summary>
    /// Generate a Target for usage in the Benchmark. This target features a
    /// fake type and method because the BenchmarkDotNet core runner prints
    /// the name of the type and method in the summary, so if these elements
    /// were null, the benchmark runner crashes with an NRE.
    /// </summary>
    private static Target GetTarget()
    {
        var type = typeof(PlaceholderBenchmarkRunner);
        var method = type.GetMethod("PlaceholderMethod");
        return new Target(type, method);
    }

    private sealed class PlaceholderBenchmarkRunner
    {
        public void PlaceholderMethod() { }
    }
}
