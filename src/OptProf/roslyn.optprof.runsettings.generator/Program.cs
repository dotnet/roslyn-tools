using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using roslyn.optprof.json;
using roslyn.optprof.lib;
using YamlDotNet.RepresentationModel;

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
                    new[] { "-c", "--configFile" },
                    "REQUIRED: The absolute path to the OptProf.json config file.",
                    c => c.WithDefaultValue(() => null).LegalFilePathsOnly().ExistingFilesOnly().ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-o", "--outputFolder" },
                    "REQUIRED: The folder to write the run settings file to.",
                    o => o.WithDefaultValue(() => null).LegalFilePathsOnly().ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-tp", "--teamProject" },
                    "optinal override, otherwise picked up from environment variables.",
                    p => p.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-rn", "--repoName" },
                    "optinal override, otherwise picked up from environment variables.",
                    r => r.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-sbn", "--sourceBranchName" },
                    "optinal override, otherwise picked up from environment variables.",
                    s => s.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-bi", "--buildId" },
                    "optinal override, otherwise picked up from environment variables.",
                    i => i.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-itb", "--insertTargetBranch" },
                    "optinal override, otherwise picked up from environment variables.",
                    b => b.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-t", "--testsUrl" },
                    "optinal override, otherwise picked up from environment variables.",
                    b => b.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddOption(
                    new[] { "-bn", "--buildNumber" },
                    "optinal override, otherwise picked up from environment variables.",
                    n => n.WithDefaultValue(() => null).ParseArgumentsAs<string>())
                .AddVersionOption()
                .OnExecute(typeof(Program).GetMethod(nameof(ExecuteAsync)))
                .Build();

            return await parser.InvokeAsync(args);
        }

        public static async Task<int> ExecuteAsync(string configFile,
                                              string outputFolder,
                                              string teamProject,
                                              string repoName,
                                              string sourceBranchName,
                                              string buildId,
                                              string insertTargetBranch,
                                              string testsUrl,
                                              string buildNumber,
                                              IConsole console = null)
        {
            await ValidateAsync(configFile, nameof(configFile), console);
            await ValidateAsync(outputFolder, nameof(outputFolder), console);
            if (configFile == null || outputFolder == null)
            {
                return 1;
            }
            var fileWriter = new FileWriter();

            using (var config = File.OpenRead(configFile))
            {
                return Execute(config, configFile, outputFolder, teamProject, repoName, sourceBranchName, buildId, insertTargetBranch, testsUrl, buildNumber, fileWriter, console);
            }
        }

        public static int Execute(Stream config,
                                  string configPath,
                                  string outputFolder,
                                  string teamProject,
                                  string repoName,
                                  string sourceBranchName,
                                  string buildId,
                                  string insertTargetBranch,
                                  string testsUrl,
                                  string buildNumber,
                                  IFileWriter fileWriter,
                                  IConsole console = null)
        {
            string dropUriString = GetDropUriString(teamProject, repoName, sourceBranchName, buildId);

            string buildUriString = GetBuildUriString(insertTargetBranch, testsUrl, buildNumber);

            var (success, testContainerString, testCaseFilterString) = GetContainerString(config);
            if (!success)
            {
                console?.Error.WriteLine($"unable to read config file '{configPath}'");
                return 1;
            }

            var runSettings = string.Format(Constants.RunSettingsTemplate, dropUriString, buildUriString, testContainerString, testCaseFilterString);

            return fileWriter.WriteOutFile(outputFolder, runSettings);
        }



        private static string GetBuildUriString(string insertTargetBranch, string testsUrl, string buildNumber)
        {
            if (testsUrl != null)
            {
                return $"vstsdrop:{testsUrl}";
            }

            if (insertTargetBranch == null)
            {
                insertTargetBranch = GetTargetBranch();
            }

            if (buildNumber == null)
            {
                (_, buildNumber) = GetBuildNumber();
            }

            string buildUriString = $"vstsdrop:Tests/DevDiv/VS/{insertTargetBranch}/{buildNumber}/x86ret";
            return buildUriString;
        }

        private static string GetDropUriString(string teamProject, string repoName, string sourceBranchName, string buildId)
        {
            if (teamProject == null)
            {
                teamProject = Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECT");
            }

            if (repoName == null)
            {
                repoName = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_NAME");
            }

            if (sourceBranchName == null)
            {
                sourceBranchName = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME");
            }

            if (buildId == null)
            {
                buildId = Environment.GetEnvironmentVariable("BUILD_BUILDID");
            }

            var dropUriString = $"vstsdrop:ProfilingInputs/{teamProject}/{repoName}/{sourceBranchName}/{buildId}";
            return dropUriString;
        }

        private static async Task ValidateAsync(string option, string optionName, IConsole console)
        {
            if (option == null && console != null)
            {
                await console.Error.WriteLineAsync($"You must specify '--{optionName}'");
            }
        }

        private static string GetTargetBranch()
        {
            var sourcesRoot = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
            var yamlFile = Path.Combine(sourcesRoot, ".vsts-ci.yml");
            using (var stream = File.OpenText(yamlFile))
            {
                var yaml = new YamlStream();
                yaml.Load(stream);
                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
                return (string)mapping["variables"]["InsertTargetBranchFullName"];
            }
        }

        private static (bool, string) GetBuildNumber()
        {
            var stagingDirectory = Environment.GetEnvironmentVariable("BUILD_STAGINGDIRECTORY");
            if (string.IsNullOrEmpty(stagingDirectory))
            {
                return (false, null);
            }

            var bootstrapperInfoPath = Path.Combine(stagingDirectory, @"MicroBuild\Output\BootstrapperInfo.json");

            using (var file = File.OpenText(bootstrapperInfoPath))
            using (var reader = new JsonTextReader(file))
            {
                var jsonContent = JObject.ReadFrom(reader);
                var parts = ((string)((JArray)jsonContent).First["VSBuildVersion"]).Split('.');
                return (true, parts[2] + "." + parts[3]);
            }
        }

        private static (bool, string, string) GetContainerString(Stream config)
        {
            using (var reader = new StreamReader(config))
            {
                return GetContainerString(reader);
            }
        }

        public static (bool, string, string) GetContainerString(StreamReader file)
        {
            var (success, config) = Config.TryReadConfigFile(file);
            if (!success)
            {
                return (false, null, null);
            }

            var containers = GetTestContainers(config);
            var filters = GetTestFilters(config);

            return (true, containers, filters);
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

        private static string GetTestFilters(json.OptProfTrainingConfiguration config)
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
