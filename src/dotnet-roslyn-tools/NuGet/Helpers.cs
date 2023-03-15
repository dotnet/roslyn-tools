// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.RoslynTools.NuGet
{
    internal static class Helpers
    {
        public static readonly string[] RoslynPackageIds =
            {
                "Microsoft.CodeAnalysis",
                "Microsoft.CodeAnalysis.Common",
                "Microsoft.CodeAnalysis.Compilers",
                "Microsoft.CodeAnalysis.CSharp",
                "Microsoft.CodeAnalysis.CSharp.CodeStyle",
                "Microsoft.CodeAnalysis.CSharp.Features",
                "Microsoft.CodeAnalysis.CSharp.Scripting",
                "Microsoft.CodeAnalysis.CSharp.Workspaces",
                "Microsoft.CodeAnalysis.EditorFeatures.Text",
                "Microsoft.CodeAnalysis.Features",
                "Microsoft.CodeAnalysis.Scripting",
                "Microsoft.CodeAnalysis.Scripting.Common",
                "Microsoft.CodeAnalysis.VisualBasic",
                "Microsoft.CodeAnalysis.VisualBasic.CodeStyle",
                "Microsoft.CodeAnalysis.VisualBasic.Features",
                "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
                "Microsoft.CodeAnalysis.Workspaces.Common",
                "Microsoft.CodeAnalysis.Workspaces.MSBuild",
                "Microsoft.Net.Compilers.Toolset",
                "Microsoft.VisualStudio.LanguageServices"
            };

        public static readonly string[] RoslynSdkPackageIds =
            {
                "Microsoft.CodeAnalysis.Analyzer.Testing",
                "Microsoft.CodeAnalysis.CodeFix.Testing",
                "Microsoft.CodeAnalysis.CodeRefactoring.Testing",
                "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing",
                "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.MSTest",
                "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.NUnit",
                "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit",
                "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing",
                "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.MSTest",
                "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.NUnit",
                "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit",
                "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing",
                "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing.MSTest",
                "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing.NUnit",
                "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing.XUnit",
                "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing",
                "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.MSTest",
                "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.NUnit",
                "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit",
                "Microsoft.CodeAnalysis.SourceGenerators.Testing",
                "Microsoft.CodeAnalysis.Testing.Verifiers.MSTest",
                "Microsoft.CodeAnalysis.Testing.Verifiers.NUnit",
                "Microsoft.CodeAnalysis.Testing.Verifiers.XUnit",
                "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing",
                "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing.MSTest",
                "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing.NUnit",
                "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing.XUnit",
                "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing",
                "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing.MSTest",
                "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing.NUnit",
                "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing.XUnit",
                "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing",
                "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing.MSTest",
                "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing.NUnit",
                "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing.XUnit",
                "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing",
                "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.MSTest",
                "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.NUnit",
                "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.XUnit"
            };

        public static bool TryDetermineRoslynPackageVersion([NotNullWhen(returnValue: true)] out string? version)
        {
            var packageFileName = Directory.GetFiles(Environment.CurrentDirectory, "Microsoft.Net.Compilers.Toolset.*.nupkg").FirstOrDefault();
            if (packageFileName is null)
            {
                version = null;
                return false;
            }

            version = Path.GetFileNameWithoutExtension(packageFileName).Substring(32);
            return true;
        }

        public static bool TryDetermineRoslynSdkPackageVersion([NotNullWhen(returnValue: true)] out string? version)
        {
            var packageFileName = Directory.GetFiles(Environment.CurrentDirectory, "Microsoft.CodeAnalysis.Analyzer.Testing.*.nupkg").FirstOrDefault();
            if (packageFileName is null)
            {
                version = null;
                return false;
            }

            version = Path.GetFileNameWithoutExtension(packageFileName).Substring(40);
            return true;
        }

        public static string GetPackageFileName(string packageId, string version) => $"{packageId}.{version}.nupkg";

    }
}
