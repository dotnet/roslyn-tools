// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;

namespace Perf;

internal sealed class ExternalProcessBuilder : IBuilder
{
    public BuildResult Build(
        GenerateResult generateResult,
        ILogger logger,
        Benchmark benchmark,
        IResolver resolver)
    {
        if (benchmark is not ExternalProcessBenchmark externalProcessBenchmark)
        {
            return BuildResult.Failure(generateResult);
        }

        var exitCode = externalProcessBenchmark.BuildFunc((string)benchmark.Parameters["Commit"]);
        return exitCode != 0 ? BuildResult.Failure(generateResult) : BuildResult.Success(generateResult);
    }
}
