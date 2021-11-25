// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
namespace SignTool
{
    internal readonly struct SignToolArgs
    {
        internal string OutputPath { get; }
        internal string IntermediateOutputPath { get; }
        internal string NuGetPackagesPath { get; }
        internal string AppPath { get; }
        internal string MSBuildPath { get; }
        internal string MSBuildBinaryLogFilePath { get; }
        internal string ConfigFile { get; }
        internal bool Test { get; }
        internal bool TestSign { get; }
        internal bool DiagnosticOutput { get; }
        internal string OrchestrationManifestPath { get; }

        internal SignToolArgs(
            string outputPath,
            string intermediateOutputPath,
            string appPath,
            string msbuildPath,
            string msbuildBinaryLogFilePath,
            string nugetPackagesPath,
            string configFile,
            bool test,
            bool testSign,
            bool diagnosticOutput,
            string orchestrationManifestPath
            )
        {
            OutputPath = outputPath;
            IntermediateOutputPath = intermediateOutputPath;
            AppPath = appPath;
            MSBuildPath = msbuildPath;
            MSBuildBinaryLogFilePath = msbuildBinaryLogFilePath;
            NuGetPackagesPath = nugetPackagesPath;
            ConfigFile = configFile;
            Test = test;
            TestSign = testSign;
            DiagnosticOutput = diagnosticOutput;
            OrchestrationManifestPath = orchestrationManifestPath;
        }
    }
}
