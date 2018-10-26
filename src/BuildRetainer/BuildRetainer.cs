// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using WindowsCredential = Microsoft.VisualStudio.Services.Common.WindowsCredential;
using System.Collections.Generic;

namespace BuildRetainer
{
    internal class BuildRetainer
    {
        private const string BranchNamePrefix = "refs/heads/";
        private const string ReleasedToPublicTag = "Released to Public";

        private Options _options;

        public BuildRetainer(Options options)
        {
            _options = options;
        }

        private class TagData
        {
            public string TagName { get; set; }

            public string ObjectId { get; set; }

            /// <summary>
            /// The branch the inserted build originated from (i.e. branch names on GitHub).
            /// </summary>
            public string InsertedBuildBranch { get; set; }

            /// <summary>
            /// The build number we inserted.
            /// </summary>
            public string InsertedBuildNumber { get; set; }

            /// <summary>
            /// The raw drop path of the VS drop.
            /// </summary>
            public string RawDropPath { get; set; }

            /// <summary>
            /// Whether the raw drop path is believed to exist. This always starts at true, and we'll switch it to false once we've
            /// observed the folder no longer exists.
            /// </summary>
            public bool MaybeExists { get; set; } = true;

            public bool IsRelease() => TagName.StartsWith("refs/tags/release/");
        }

        public void Run()
        {
            Console.WriteLine($"Retaining builds from [{_options.BuildQueueName}] for component [{_options.ComponentName}].");

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Getting KeyVault secret...");
            var password = GetSecret(Settings.Default.VsoSecretName, _options).Result;

            var uri = new Uri(Settings.Default.VSTSUrl);
            var connection = new VssConnection(uri, new WindowsCredential(new NetworkCredential(Settings.Default.UserName, password)));

            Console.WriteLine("Authenticating connection...");
            using (var gitHttpClient = connection.GetClient<GitHttpClient>())
            {
                Console.WriteLine("Finding repository...");
                var allRepos = gitHttpClient.GetRepositoriesAsync().Result;
                var vsRepos = allRepos.Where(r => r.Name == Settings.Default.RepositoryName && r.ProjectReference.Name == Settings.Default.TFSProjectName).ToList();
                if (vsRepos.Count != 1)
                {
                    Console.WriteLine($"Expected to find one repository matching {Settings.Default.TFSProjectName}/{Settings.Default.RepositoryName} but found {vsRepos.Count}.");
                    Environment.Exit(1);
                }

                var vsRepo = vsRepos.Single();
                var vsRepoGuid = vsRepo.Id;
                Console.WriteLine("Getting all tags...");
                var tagRefs = gitHttpClient.GetTagRefsAsync(vsRepoGuid).Result;

                var allTagData = ReadCachedTags();

                (int numSkipped, int numProcessed, int numFailed, int numCached) operationTracker = (0, 0, 0, 0);

                Console.WriteLine();
                Console.WriteLine($"Processing {tagRefs.Count} tags...");
                Parallel.ForEach(tagRefs, (tagRef) =>
                {
                    try
                    {
                        TagData tag;

                        if (allTagData.TryGetValue(tagRef.Name, out tag) && tag.ObjectId == tagRef.ObjectId)
                        {
                            Interlocked.Increment(ref operationTracker.numCached);

                            // We already have the tag and it's data. Let's recheck if it's been expired
                            if (tag.MaybeExists)
                            {
                                tag.MaybeExists = Directory.Exists(tag.RawDropPath);
                            }

                            return;
                        }

                        tag = new TagData();
                        tag.TagName = tagRef.Name;

                        if (!Regex.IsMatch(tagRef.Name, @"refs/tags/drop/.*/official\.\d{5}.\d{2}") && !tag.IsRelease())
                        {
                            Interlocked.Increment(ref operationTracker.numSkipped);
                            return;
                        }

                        tag.ObjectId = tagRef.ObjectId;

                        var versionDescriptor = new GitVersionDescriptor()
                        {
                            Version = tagRef.Name.Substring("refs/tags/".Length),
                            VersionOptions = GitVersionOptions.None,
                            VersionType = GitVersionType.Tag
                        };

                        var annotatedTagDataTask = gitHttpClient.GetAnnotatedTagAsync(Settings.Default.TFSProjectName, vsRepoGuid, tagRef.ObjectId);
                        var annotatedTagData = JObject.Parse(annotatedTagDataTask.Result.Message);

                        tag.RawDropPath = annotatedTagData["RawDropLocation"].Value<string>();
                        tag.MaybeExists = Directory.Exists(tag.RawDropPath);

                        // If the build is already gone, no reason to get the components.json for it
                        if (tag.MaybeExists)
                        {
                            if (!TryUpdateTag(gitHttpClient, vsRepoGuid, tag, versionDescriptor, @".corext\Configs\components.json") &&
                                !TryUpdateTag(gitHttpClient, vsRepoGuid, tag, versionDescriptor, @".corext\Configs\dotnetcodeanalysis-components.json") &&
                                !TryUpdateTag(gitHttpClient, vsRepoGuid, tag, versionDescriptor, @".corext\Configs\lutandsbd-components.json") &&
                                !TryUpdateTag(gitHttpClient, vsRepoGuid, tag, versionDescriptor, @".corext\Configs\dotnetprojectsystem-components.json"))
                            {
                                throw new Exception("Unable to locate component.");
                            }
                        }

                        if (!allTagData.TryAdd(tagRef.Name, tag))
                        {
                            throw new Exception("Tag already exists which shouldn't have happened.");
                        }

                        Interlocked.Increment(ref operationTracker.numProcessed);
                    }
                    catch (Exception)
                    {
                        // Catastrophic error: don't cache anything since we have no idea what happened
                        allTagData.TryRemove(tagRef.Name, out _);
                        Interlocked.Increment(ref operationTracker.numFailed);
                    }
                });

                Console.WriteLine($"    Processed {operationTracker.numProcessed + operationTracker.numSkipped + operationTracker.numFailed + operationTracker.numCached} tags. {operationTracker.numProcessed} were new, {operationTracker.numSkipped} were skipped, {operationTracker.numFailed} failed to be interpreted, and {operationTracker.numCached} where previously processed.");

                WriteCachedTags(allTagData);

                Console.WriteLine();
                Console.WriteLine("Locating unique builds...");

                // Figure out all our builds that have been inserted, and which release tags if they happen to be release tags
                var uniqueInsertedBuilds = (from tag in allTagData.Values
                                            where tag.MaybeExists
                                            group tag by new { tag.InsertedBuildBranch, tag.InsertedBuildNumber } into g
                                            select new { g.Key.InsertedBuildBranch, g.Key.InsertedBuildNumber, ReleaseTags = g.Where(t => t.IsRelease()) })
                                            .Distinct().ToList();

                Console.WriteLine($"    Found {uniqueInsertedBuilds.Count} unique builds.");

                Console.WriteLine();
                var currentBranch = string.Empty;

                foreach (var insertedBuildsByBranch in uniqueInsertedBuilds.GroupBy(b => b.InsertedBuildBranch))
                {
                    Console.WriteLine(insertedBuildsByBranch.Key);

                    foreach (var build in insertedBuildsByBranch.OrderBy(b => b.InsertedBuildNumber))
                    {
                        Console.Write($"    {build.InsertedBuildNumber}");

                        if (build.ReleaseTags.Any())
                        {
                            Console.Write($" (released to public as {string.Join(", ", build.ReleaseTags.Select(t => t.TagName).OrderBy(t => t))})");
                        }

                        Console.WriteLine();
                    }
                }

                using (var buildClient = connection.GetClient<BuildHttpClient>())
                {
                    Console.WriteLine("Finding build definition");
                    var projectId = vsRepo.ProjectReference.Id;
                    var definitions = buildClient.GetDefinitionsAsync(projectId).Result;
                    var buildDefinitions = definitions.Where(d => d.Name == _options.BuildQueueName && d.DefinitionQuality != DefinitionQuality.Draft).ToList();
                    if (buildDefinitions.Count != 1)
                    {
                        throw new Exception($"Expected one build definition named {_options.BuildQueueName} but found {buildDefinitions.Count}.");
                    }

                    var buildDefinitionIds = new int[]
                    {
                        buildDefinitions.Single().Id
                    };

                    var builds = buildClient.GetBuildsAsync(projectId, buildDefinitionIds, statusFilter: BuildStatus.Completed).Result;
                    var modifiedBuilds = new HashSet<Build>();

                    Console.WriteLine();
                    Console.WriteLine("Determining which builds should be marked for retention...");
                    foreach (var build in builds)
                    {
                        var insertedBuild = uniqueInsertedBuilds.FirstOrDefault(b => NormalizeBranchName(b.InsertedBuildBranch) == NormalizeBranchName(build.SourceBranch) &&
                                                                                     b.InsertedBuildNumber == build.BuildNumber);

                        if (insertedBuild != null && insertedBuild.ReleaseTags.Any() && !build.Tags.Contains(ReleasedToPublicTag))
                        {
                            buildClient.AddBuildTagAsync(projectId, build.Id, ReleasedToPublicTag).Wait();
                        }

                        if (insertedBuild != null || build.Tags.Contains(ReleasedToPublicTag))
                        {
                            if (!build.KeepForever.GetValueOrDefault())
                            {
                                Console.WriteLine($"Adding '{NormalizeBranchName(build.SourceBranch)} - {build.BuildNumber}' to the list of builds to keep forever.");
                                build.KeepForever = true;
                                modifiedBuilds.Add(build);
                            }
                        }
                        else if (build.KeepForever.GetValueOrDefault())
                        {
                            // no longer needed
                            Console.WriteLine($"Removing '{NormalizeBranchName(build.SourceBranch)} - {build.BuildNumber}' from the list of builds to keep forever.");
                            build.KeepForever = false;
                            modifiedBuilds.Add(build);
                        }
                    }
                    Console.WriteLine($"    Found {modifiedBuilds.Count} builds which need their retention setting modified.");
                    Console.WriteLine();

                    if (modifiedBuilds.Count > 0)
                    {
                        buildClient.UpdateBuildsAsync(modifiedBuilds, projectId).Wait();
                    }
                }

                stopwatch.Stop();

                Console.WriteLine();
                Console.WriteLine($"Total Time: {stopwatch.Elapsed.TotalSeconds} seconds");
            }
        }

        private bool TryUpdateTag(GitHttpClient gitHttpClient, Guid vsRepoGuid, TagData tag, GitVersionDescriptor versionDescriptor, string componentsJsonFile)
        {
            Stream itemContentStream;

            try
            {
                itemContentStream = gitHttpClient.GetItemContentAsync(Settings.Default.TFSProjectName, vsRepoGuid, componentsJsonFile, versionDescriptor: versionDescriptor).Result;
            }
            catch (AggregateException e) when (e.InnerException.Message.Contains("TF401174")) // item could not be found
            {
                // That file doesn't exist in this drop;
                return false;
            }

            using (itemContentStream)
            using (var jsonReader = new JsonTextReader(new StreamReader(itemContentStream)))
            {
                var jsonDocument = (JObject)(JToken.ReadFrom(jsonReader));

                var components = jsonDocument["Components"];
                var component = components[_options.ComponentName];

                if (component == null)
                {
                    return false;
                }

                var componentUrl = component["url"].Value<string>();
                var componentParts = componentUrl.Split(';').First().Split('/');

                tag.InsertedBuildBranch = componentParts[componentParts.Length - 2];
                tag.InsertedBuildNumber = componentParts[componentParts.Length - 1];

                // Just to protect against us getting something bad, assert the build number is numeric
                if (!Regex.IsMatch(tag.InsertedBuildNumber, "^[0-9.]+$"))
                {
                    throw new Exception($"{tag.InsertedBuildNumber} doesn't look numeric.");
                }
            }

            return true;
        }

        private static string NormalizeBranchName(string branchName)
        {
            return branchName.StartsWith(BranchNamePrefix)
                ? branchName.Substring(BranchNamePrefix.Length)
                : branchName;
        }

        /// <summary>
        /// Gets the specified secret from the key vault.
        /// </summary>
        private async Task<string> GetSecret(string secretName, Options options)
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenFunction(options.ClientId, options.ClientSecret)));
            var secret = await kv.GetSecretAsync(Settings.Default.KeyVaultUrl, secretName);
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
                    authResult = await context.AcquireTokenAsync(resource, Settings.Default.ApplicationId, new UserCredential());
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

        private ConcurrentDictionary<string, TagData> ReadCachedTags()
        {
            var cachedTags = new ConcurrentDictionary<string, TagData>();
            var cachedTagDataFileName = $@"ProcessedData.{_options.ComponentName}_v2.txt";

            using (var blobStream = GetReadableAzureStream(cachedTagDataFileName).Result)
            {
                using (var cachedTagDataFileStream = new StreamReader(blobStream))
                {
                    while (!cachedTagDataFileStream.EndOfStream)
                    {
                        var currentLine = cachedTagDataFileStream.ReadLine();
                        var tagData = JsonConvert.DeserializeObject<TagData>(currentLine);

                        cachedTags.TryAdd(tagData.TagName, tagData);
                    }
                }
            }

            return cachedTags;
        }

        private void WriteCachedTags(ConcurrentDictionary<string, TagData> cachedTags)
        {
            var cachedTagDataFileName = $@"ProcessedData.{_options.ComponentName}_v2.txt";
            using (var blobStream = GetWritableAzureStream(cachedTagDataFileName).Result)
            {
                using (var cachedTagDataFileStream = new StreamWriter(blobStream))
                {
                    foreach (var tag in cachedTags.Values)
                    {
                        cachedTagDataFileStream.WriteLine(JsonConvert.SerializeObject(tag));
                    }
                }
            }
        }

        /// <summary>
        /// Get a readable stream to an Azure storage blob.
        /// </summary>
        private async Task<Stream> GetReadableAzureStream(string path)
        {
            var blob = await GetBlob(path);

            // the reader code is made much simpler if there's always a stream present, even if it's empty
            return blob.Exists() ? await blob.OpenReadAsync() : new MemoryStream();
        }

        /// <summary>
        /// Get a writable stream to an Azure storage blob.
        /// </summary>
        private async Task<Stream> GetWritableAzureStream(string path)
        {
            var blob = await GetBlob(path);
            return await blob.OpenWriteAsync();
        }

        private async Task<CloudBlockBlob> GetBlob(string path)
        {
            var blobToken = await GetSecret(Settings.Default.BlobStorageSecretName, _options);
            var credentials = new StorageCredentials(Settings.Default.BlobStorageAccountName, blobToken);
            var storageAccount = new CloudStorageAccount(credentials, useHttps: true);
            var container = storageAccount.CreateCloudBlobClient().GetContainerReference(Settings.Default.BlobContainerName);
            var blob = container.GetBlockBlobReference(path);
            return blob;
        }
    }
}
