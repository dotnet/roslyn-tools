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
                .AddOption(
                    new[] { "-yf", "--yamlFileName" },
                    "optinal override, otherwise uses .vsts-ci.yml",
                    n => n.WithDefaultValue(() => ".vsts-ci.yml").ParseArgumentsAs<string>())
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
                                              string yamlFileName,
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
                return Execute(config, configFile, outputFolder, teamProject, repoName, sourceBranchName, buildId, insertTargetBranch, testsUrl, buildNumber, yamlFileName, fileWriter, console);
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
                                  string yamlFileName,
                                  IFileWriter fileWriter,
                                  IConsole console = null)
        {
            var dropUriString = GetDropUriString(teamProject, repoName, sourceBranchName, buildId);

            var buildUriString = GetBuildUriString(insertTargetBranch, testsUrl, buildNumber, yamlFileName);

            var (success, testContainerString, testCaseFilterString) = GetContainerString(config);
            if (!success)
            {
                console?.Error.WriteLine($"unable to read config file '{configPath}'");
                return 1;
            }

            var runSettings = string.Format(Constants.RunSettingsTemplate, dropUriString, buildUriString, testContainerString, testCaseFilterString);

            return fileWriter.WriteOutFile(outputFolder, runSettings);
        }



        private static string GetBuildUriString(string insertTargetBranch, string testsUrl, string buildNumber, string yamlFileName)
        {
            bool success;
            if (testsUrl != null)
            {
                return $"vstsdrop:{testsUrl}";
            }
            else
            {
                (success, testsUrl) = GetTestsUrl();
            }

            if (success)
            {
                return testsUrl;
            }

            if (insertTargetBranch == null)
            {
                insertTargetBranch = GetTargetBranch(yamlFileName);
            }

            if (buildNumber == null)
            {
                (_, buildNumber) = GetBuildNumber();
            }

            var buildUriString = $"vstsdrop:Tests/DevDiv/VS/{insertTargetBranch}/{buildNumber}/x86ret";
            return buildUriString;
        }

        public static (bool, string) GetTestsUrl(string bootstrapperInfoPath = null)
        {
            if (bootstrapperInfoPath == null)
            {
                var stagingDirectory = Environment.GetEnvironmentVariable("BUILD_STAGINGDIRECTORY");
                if (string.IsNullOrEmpty(stagingDirectory))
                {
                    return (false, null);
                }

                bootstrapperInfoPath = Path.Combine(stagingDirectory, @"MicroBuild\Output\BootstrapperInfo.json");
            }

            using (var file = File.OpenText(bootstrapperInfoPath))
                return GetTestsUrl(file);
        }

        public static (bool, string) GetTestsUrl(StreamReader file)
        {
            try
            {
                using (var reader = new JsonTextReader(file))
                {
                    var jsonContent = JToken.ReadFrom(reader);
                    var buildDropPath = (string)((JArray)jsonContent).First["BuildDrop"];
                    if (buildDropPath.Contains("/Products/") && buildDropPath.Contains("https://vsdrop.corp.microsoft.com/file/v1/"))
                    {
                        var testsUri = $"vstsdrop:{buildDropPath.Replace("/Products/", "/Tests/").Substring("https://vsdrop.corp.microsoft.com/file/v1/".Length)}";
                        return (true, testsUri);
                    }
                    else
                    {
                        return (false, null);
                    }
                }
            }
            catch (Exception)
            {
                return (false, null);
            }
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

        private static string GetTargetBranch(string yamlFileName)
        {
            var sourcesRoot = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
            var yamlFile = Path.Combine(sourcesRoot, yamlFileName);
            using (var stream = File.OpenText(yamlFile))
            {
                var yaml = new YamlStream();
                yaml.Load(stream);
                return GetTargetBranch(yaml);
            }
        }

        public static string GetTargetBranch(YamlStream yaml)
        {
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            // The "variables" node can be stored in two places: root of YAML and under the
            // jobs+jobs path. Need to look in both places here.
            var branchNode = tryGetBranch(mapping);
            if (branchNode != null)
            {
                return (string)branchNode;
            }

            if (mapping.Children.TryGetValue("jobs", out var node) && node is YamlSequenceNode jobs)
            {
                foreach (var job in jobs.OfType<YamlMappingNode>())
                {
                    branchNode = tryGetBranch(job);
                    if (branchNode != null)
                    {
                        return (string)branchNode;
                    }
                }
            }

            throw new Exception("Unable to calculate the target branch");

            YamlNode tryGetBranch(YamlMappingNode yamlNode)
            {
                return tryGetPath(yamlNode, "variables", "InsertTargetBranchFullName");
            }

            YamlNode tryGetPath(YamlMappingNode yamlNode, params string[] path)
            {
                YamlNode current = yamlNode;
                for (var i = 0; i < path.Length; i++)
                {
                    if (!(current is YamlMappingNode mappingNode))
                    {
                        return null;
                    }

                    if (!mappingNode.Children.TryGetValue(path[i], out current))
                    {
                        return null;
                    }
                }

                return current;
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
                var jsonContent = JToken.ReadFrom(reader);
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
