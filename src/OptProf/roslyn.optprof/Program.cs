using Mono.Options;
using Newtonsoft.Json.Linq;
using roslyn.optprof.json;
using roslyn.optprof.lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace roslyn.optprof
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            bool showHelp = false;
            string configFile = null;
            string rootFolder = null;
            string outputFolder = null;

            var parser = new OptionSet
            {
                {
                    "h|?|help",
                    "Show help.",
                    h => showHelp = h != null
                },
                {
                    "c|config=",
                    "The absolute path to the OptProf.json config file.",
                    c => configFile = c
                },
                {
                    "if=|insertionfolder=",
                    "This is the absolute path to the folder that contains the VSIXes that will be inserted.",
                    i => rootFolder = i
                },
                {
                    "o=|output=",
                    $"The folder to output the results optprof data to.",
                    o => outputFolder = o
                },
            };

            List<string> extraArguments = null;
            try
            {
                extraArguments = parser.Parse(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to parse arguments.");
                Console.WriteLine(e.Message);
                return;
            }

            if (extraArguments.Count > 0)
            {
                Console.WriteLine($"Unknown arguments: {string.Join(" ", extraArguments)}");
                return;
            }

            if (showHelp)
            {
                parser.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (configFile == null)
            {
                Console.WriteLine($"Must specify '-config'");
                parser.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (rootFolder == null)
            {
                Console.WriteLine($"Must specify '-insertionfolder'");
                parser.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (outputFolder == null)
            {
                Console.WriteLine($"Must specify '-output'");
                parser.WriteOptionDescriptions(Console.Out);
                return;
            }

            Execute(configFile, rootFolder, outputFolder);
        }

        private static void Execute(string configFile, string rootFolder, string outputFolder)
        {
            var config = ReadConfigFile(configFile);

            foreach (var product in config.Products)
            {
                string productName = product.Name;
                string path = Path.Combine(rootFolder, productName);
                using (var vsix = Vsix.Create(path))
                {
                    var jsonManifest = vsix.ParseJsonManifest("/manifest.json");
                    var fileEntries = Manifest.GetNgenEntriesFromJsonManifest(jsonManifest).ToArray();
                    foreach (var test in product.Tests)
                    {
                        var folder = Path.Combine(outputFolder, test.Container);
                        var configurations = Path.Combine(folder, "Configurations");
                        foreach (var fullyQualifiedName in test.TestCases)
                        {
                            var folderToWriteJsonEntires = Path.Combine(configurations, fullyQualifiedName);
                            if (!Directory.Exists(folderToWriteJsonEntires))
                            {
                                Directory.CreateDirectory(folderToWriteJsonEntires);
                            }

                            foreach (var entry in fileEntries)
                            {
                                var filename = Path.GetFileNameWithoutExtension(entry.RelativeInstallationPath) + ".IBC.json";
                                using (var writer = new StreamWriter(File.Open(Path.Combine(folderToWriteJsonEntires, filename), FileMode.Create, FileAccess.Write, FileShare.Read)))
                                {
                                    string jsonString = ToJsonString(entry);
                                    writer.WriteLine(jsonString);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static OptProfTrainingConfiguration ReadConfigFile(string pathToConfigFile)
        {
            using (var file = File.OpenText(pathToConfigFile))
            {
                var (success, config) = Config.TryReadConfigFile(file);
                if (!success)
                {
                    // handle error case
                    throw new Exception("Unable to open the config file");
                }

                return config;
            }
        }

        private static string ToJsonString((string Technology, string RelativeInstallationPath, string InstrumentationArguments) entry)
        {
            return new JObject(
                new JProperty("Technology", entry.Technology),
                new JProperty("RelativeInstallationPath", entry.RelativeInstallationPath),
                new JProperty("InstrumentationArguments", entry.InstrumentationArguments)).ToString();
        }
    }
}
