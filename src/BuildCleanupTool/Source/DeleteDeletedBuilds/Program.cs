using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DeleteDeletedBuilds
{
    static class Program
    {
        static void Main(string[] args)
        {
            var client = new BuildHttpClient(new Uri("https://devdiv.visualstudio.com"), new VssBasicCredential("", GetSecret("vslsnap-vso-auth-token").Result));
            int buildsProcessed = 0;

            var definitions = new List<BuildDefinitionReference>();
            definitions.AddRange(client.GetDefinitionsAsync(project: "DevDiv", name: "Roslyn-Signed-Legacy-15.6AndEarlier").Result);
            definitions.AddRange(client.GetDefinitionsAsync(project: "DevDiv", name: "Roslyn-Signed").Result);
            definitions.AddRange(client.GetDefinitionsAsync(project: "DevDiv", name: "TestImpact-Signed").Result);

            foreach (var definition in definitions)
            {
                var folderName = definition.Name == "Roslyn-Signed-Legacy-15.6AndEarlier" ? "Roslyn-Signed" : definition.Name;

                string dropBase = $@"\\cpvsbuild\drops\roslyn\{folderName}";

                var builds = client.GetBuildsAsync(definition.Project.Id, definitions: new[] { definition.Id }, deletedFilter: QueryDeletedOption.IncludeDeleted).Result;

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
                                var logs = client.GetBuildLogsAsync(definition.Project.Id, build.Id).Result;
                                var artifacts = client.GetArtifactsAsync(definition.Project.Id, build.Id).Result;

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
        }

        private static void DeleteIfEmpty(this DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.GetFileSystemInfos().Any())
            {
                directoryInfo.Delete(recursive: false);
            }
        }

        /// <summary>
        /// Gets the specified secret from the key vault;
        /// </summary>
        private static async Task<string> GetSecret(string secretName)
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
            var secret = await kv.GetSecretAsync("https://roslyninfra.vault.azure.net/", secretName);
            return secret.Value;
        }

        private static async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            // use default domain authentication
            var context = new AuthenticationContext(authority);
            return (await context.AcquireTokenAsync(resource, "1950a258-227b-4e31-a9cf-717495945fc2", new UserCredential())).AccessToken;
        }
    }
}
