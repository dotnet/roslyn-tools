using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Mono.Options;

namespace DeleteDeletedBuilds
{
    static class Program
    {
        const string ApplicationId = "1950a258-227b-4e31-a9cf-717495945fc2";
        const string KeyVaultUrl = "https://roslyninfra.vault.azure.net:443";

        static async Task<int> Main(string[] args)
        {
            string clientId = "";
            string clientSecret = "";
            bool showHelp = false;
            var parser = new OptionSet
            {
                {
                    "h|?|help",
                    "Show help.",
                    h => showHelp = h != null
                },
                {
                    "ci=|clientid=",
                    "The client ID to use for authentication token retreival.",
                    id => clientId = id
                },
                {
                    "cs=|clientsecret=",
                    "The client secret to use for authentication token retreival.",
                    secret => clientSecret = secret
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
                return 1;
            }

            if (extraArguments.Count > 0)
            {
                Console.WriteLine($"Unknown arguments: {string.Join(" ", extraArguments)}");
                return 1;
            }

            if (showHelp)
            {
                parser.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            try
            {
                var authToken = await GetSecret("vslsnap-vso-auth-token", clientId, clientSecret);
                var client = new BuildHttpClient(new Uri("https://devdiv.visualstudio.com"), new VssBasicCredential("", authToken));
                int buildsProcessed = 0;

                var definitions = new List<BuildDefinitionReference>();
                definitions.AddRange(await client.GetDefinitionsAsync(project: "DevDiv", name: "Roslyn-Signed-Legacy-15.6AndEarlier"));
                definitions.AddRange(await client.GetDefinitionsAsync(project: "DevDiv", name: "Roslyn-Signed"));
                definitions.AddRange(await client.GetDefinitionsAsync(project: "DevDiv", name: "TestImpact-Signed"));

                foreach (var definition in definitions)
                {
                    var folderName = definition.Name == "Roslyn-Signed-Legacy-15.6AndEarlier" ? "Roslyn-Signed" : definition.Name;

                    string dropBase = $@"\\cpvsbuild\drops\roslyn\{folderName}";

                    var builds = await client.GetBuildsAsync(definition.Project.Id, definitions: new[] { definition.Id }, deletedFilter: QueryDeletedOption.IncludeDeleted);

                    foreach (var build in builds)
                    {
                        if (build.Status == BuildStatus.Completed)
                        {
                            string dropPath = Path.Combine(dropBase, Path.GetFileName(build.SourceBranch), "Release", build.BuildNumber);

                            if (Directory.Exists(dropPath))
                            {
                                bool deleted = build.Deleted;

                                if (!deleted)
                                {
                                    // HACK: if your retention policy says to keep the build record (but delete everything else),
                                    // you can't rely on build.Deleted.
                                    var logs = await client.GetBuildLogsAsync(definition.Project.Id, build.Id);
                                    var artifacts = await client.GetArtifactsAsync(definition.Project.Id, build.Id);

                                    if (logs == null && artifacts.Count == 0)
                                    {
                                        deleted = true;
                                    }
                                }

                                if (deleted)
                                {
                                    buildsProcessed++;
                                    Console.WriteLine($"Deleting {dropPath}...");
                                    try
                                    {
                                        Directory.Delete(dropPath, recursive: true);
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        Console.WriteLine("ACCESS EXCEPTION!");
                                    }
                                    catch (DirectoryNotFoundException e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                    catch (IOException e)
                                    {
                                        Console.WriteLine(e.ToString());
                                    }
                                }
                            }
                        }
                    }

                    /*
                    // Also clean up any now empty branch folders
                    foreach (var branchFolder in new DirectoryInfo(dropBase).GetDirectories())
                    {
                        var releaseFolder = branchFolder.GetDirectories("Release").SingleOrDefault();

                        if (releaseFolder != null)
                        {
                            releaseFolder.DeleteIfEmpty();
                        }

                        branchFolder.DeleteIfEmpty();
                    }
                    */
                }

                Console.WriteLine($"Builds processed: {buildsProcessed}");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }

        private static void DeleteIfEmpty(this DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.GetFileSystemInfos().Any())
            {
                directoryInfo.Delete(recursive: false);
            }
        }

        private static async Task<string> GetSecret(string secretName, string clientId, string clientSecret)
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenFunction(clientId, clientSecret)));
            var secret = await kv.GetSecretAsync(KeyVaultUrl, secretName);
            return secret.Value;
        }

        private static Func<string, string, string, Task<string>> GetAccessTokenFunction(string clientId, string clientSecret)
        {
            return async (authority, resource, scope) =>
            {
                var context = new AuthenticationContext(authority);
                AuthenticationResult authResult;
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    // use default domain authentication
                    authResult = await context.AcquireTokenAsync(resource, ApplicationId, new UserCredential());
                }
                else
                {
                    // use client authentication from command line arguments
                    var credentials = new ClientCredential(clientId, clientSecret);
                    authResult = await context.AcquireTokenAsync(resource, credentials);
                }

                return authResult.AccessToken;
            };
        }
    }
}
