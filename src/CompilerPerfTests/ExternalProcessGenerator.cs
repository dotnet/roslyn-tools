﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;

namespace Perf;

internal sealed class ExternalProcessGenerator(string exePath) : IGenerator
{
    private readonly ArtifactsPaths _artifactsPaths = new(
        rootArtifactsFolderPath: "",
        buildArtifactsDirectoryPath: "",
        binariesDirectoryPath: "",
        programCodePath: "",
        appConfigPath: "",
        projectFilePath: "",
        buildScriptFilePath: "",
        executablePath: exePath,
        programName: Path.GetFileName(exePath));

    public GenerateResult GenerateProject(Benchmark benchmark, ILogger logger, string rootArtifactsFolderPath, IConfig config, IResolver resolver)
    {
        return benchmark is not ExternalProcessBenchmark ? GenerateResult.Failure(null, Array.Empty<string>()) : GenerateResult.Success(_artifactsPaths, Array.Empty<string>());
    }
}
