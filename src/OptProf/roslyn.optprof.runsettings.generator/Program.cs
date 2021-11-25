// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using roslyn.optprof.json;
using roslyn.optprof.lib;

namespace roslyn.optprof.runsettings.generator
{
    public class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder()
                .UseParseDirective()
                .UseHelp()
                .UseSuggestDirective()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(
                    new[] { "-c", "--config" },
                    "REQUIRED: The absolute path to the OptProf.json config file.",
                    c => c.WithDefaultValue(() => null).LegalFilePathsOnly().ExistingFilesOnly().ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-o", "--out" },
                    "REQUIRED: The file path to write the run settings to (e.g. 'RoslynOptProf.runsettings').",
                    o => o.WithDefaultValue(() => null).LegalFilePathsOnly().ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-d", "--vsDropName" },
                    "REQUIRED: Product drop name, e.g. 'Products/$(System.TeamProject)/$(Build.Repository.Name)/$(Build.SourceBranchName)/$(Build.BuildNumber)'",
                    p => p.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-b", "--bootstrapperInfo" },
                    "REQUIRED: Path to the BootstrapperInfo.json",
                    b => b.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .OnExecute(typeof(Program).GetMethod(nameof(ExecuteAsync)))
                .Build();

            return await parser.InvokeAsync(args);
        }

        public static Task<int> ExecuteAsync(
            string config,
            string @out,
            string vsDropName,
            string bootstrapperInfo,
            IConsole console = null)
        {
            try
            {
                RequireValue(config, nameof(config));
                RequireValue(@out, nameof(@out));
                RequireValue(vsDropName, nameof(vsDropName));
                RequireValue(bootstrapperInfo, nameof(bootstrapperInfo));

                WriteOutFile(@out, GenerateRunSettings(config, vsDropName, bootstrapperInfo));
                return Task.FromResult(0);
            }
            catch (Exception e)
            {
                console?.Error.WriteLine(e.Message);
                return Task.FromResult(1);
            }
        }

        private static void RequireValue(string value, string argumentName)
        {
            if (value == null)
            {
                throw new ApplicationException($"Argument required: --{argumentName}");
            }
        }

        public static void WriteOutFile(string filePath, string content)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, content);
        }

        public static string GenerateRunSettings(
            string configPath,
            string vsDropName,
            string bootstrapperInfoPath)
        {
            var profilingInputsDropName = GetProfilingInputsDropName(vsDropName);
            var buildDropName = GetTestsDropName(File.ReadAllText(bootstrapperInfoPath, Encoding.UTF8));
            var (testContainerString, testCaseFilterString) = GetContainerString(File.ReadAllText(configPath), configPath);

            return string.Format(
                Constants.RunSettingsTemplate,
                "vstsdrop:" + profilingInputsDropName,
                "vstsdrop:" + buildDropName,
                testContainerString,
                testCaseFilterString);
        }

        public static string GetTestsDropName(string bootstrapperInfoJson)
        {
            try
            {
                var jsonContent = JToken.Parse(bootstrapperInfoJson);
                var dropUrl = (string)((JArray)jsonContent).First["BuildDrop"];

                const string prefix = "https://vsdrop.corp.microsoft.com/file/v1/Products/";
                if (!dropUrl.StartsWith(prefix, StringComparison.Ordinal))
                {
                    throw new ApplicationException($"Invalid drop URL: '{dropUrl}'");
                }

                return $"Tests/{dropUrl.Substring(prefix.Length)}";
            }
            catch (Exception e)
            {
                throw new InvalidDataException(
                    $"Unable to read boostrapper info: {e.Message}{Environment.NewLine}" +
                    $"Content of BootstrapperInfo.json:{Environment.NewLine}" +
                    $"{bootstrapperInfoJson}");
            }
        }

        private static string GetProfilingInputsDropName(string vsDropName)
        {
            const string prefix = "Products/";
            if (!vsDropName.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new ApplicationException("Invalid value of vsDropName argument: must start with 'Products/'.");
            }

            return "ProfilingInputs/" + vsDropName.Substring(prefix.Length);
        }

        public static (string containers, string filters) GetContainerString(string configJson, string configPath)
        {
            try
            {
                var config = Config.ReadConfigFile(configJson);
                return (GetTestContainers(config), GetTestFilters(config));
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Unable to read config file '{configPath}': {e.Message}");
            }
        }

        private static string GetTestContainers(OptProfTrainingConfiguration config)
        {
            var productContainers = config.Products?.Any() == true
              ? config.Products.SelectMany(x => x.Tests.Select(y => y.Container + ".dll"))
              : Enumerable.Empty<string>();

            var assemblyContainers = config.Assemblies?.Any() == true
                ? config.Assemblies.SelectMany(x => x.Tests.Select(y => y.Container + ".dll"))
                : Enumerable.Empty<string>();

            return string.Join(
                Environment.NewLine,
                productContainers
                    .Concat(assemblyContainers)
                    .Distinct()
                    .Select(x => $@"<TestContainer FileName=""{x}"" />"));
        }

        private static string GetTestFilters(OptProfTrainingConfiguration config)
        {
            var productTests = config.Products?.Any() == true
                ? config.Products.SelectMany(x => x.Tests.SelectMany(y => y.TestCases))
                : Enumerable.Empty<string>();

            var assemblyTests = config.Assemblies?.Any() == true
                ? config.Assemblies.SelectMany(x => x.Tests.SelectMany(y => y.TestCases))
                : Enumerable.Empty<string>();

            return string.Join(
                "|",
                productTests
                    .Concat(assemblyTests)
                    .Distinct()
                    .Select(x => $"FullyQualifiedName={x}"));
        }
    }
}
