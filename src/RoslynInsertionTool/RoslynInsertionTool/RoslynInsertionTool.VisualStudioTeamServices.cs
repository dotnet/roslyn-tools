// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static readonly Lazy<TfsTeamProjectCollection> LazyProjectCollection = new Lazy<Microsoft.TeamFoundation.Client.TfsTeamProjectCollection>(() =>
        {
            Log.Trace($"Creating TfsTeamProjectCollection object from {Options.VSTSUri}");
            return new TfsTeamProjectCollection(new Uri(Options.VSTSUri), new VssBasicCredential(Options.Username, Options.Password));
        });

        private static TfsTeamProjectCollection ProjectCollection => LazyProjectCollection.Value;

        private static GitPullRequest CreatePullRequest(string sourceBranch, string targetBranch, string description)
        {
            Log.Trace($"Creating pull request sourceBranch:{sourceBranch} targetBranch:{targetBranch} description:{description}");
            return new GitPullRequest
            {
                Title = $"{Options.InsertionName} Insertion Into {Options.VisualStudioBranchName} ",
                Description = description,
                SourceRefName = sourceBranch,
                TargetRefName = targetBranch
            };
        }

        private static async Task<GitPullRequest> CreatePullRequestAsync(string branchName, string message, CancellationToken cancellationToken)
        {
            var gitClient = ProjectCollection.GetClient<GitHttpClient>();
            Log.Trace($"Getting remote repository from {Options.VisualStudioBranchName} in {Options.TFSProjectName}");
            var repository = await gitClient.GetRepositoryAsync(project: Options.TFSProjectName, repositoryId: "VS", cancellationToken: cancellationToken);
            return await gitClient.CreatePullRequestAsync(
                    CreatePullRequest("refs/heads/" + branchName, "refs/heads/" + Options.VisualStudioBranchName, message),
                    repository.Id,
                    cancellationToken);
        }

        private static async Task<Build> GetLatestPassedBuildAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Log.Trace($"Getting latest passing build from {Options.TFSProjectName} where name is {Options.RoslynBuildQueueName}");
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.RoslynBuildQueueName);
            var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken, BuildResult.Succeeded);
            return (from build in builds
                    orderby build.FinishTime descending
                    select build).FirstOrDefault();
        }

        private static async Task<IEnumerable<Build>> GetBuildsFromTFSAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, CancellationToken cancellationToken, BuildResult? resultFilter = default(BuildResult))
        {
            IEnumerable<Build> builds = await GetBuildsFromTFSByBranchAsync(buildClient, definitions, Options.RoslynBranchName, resultFilter, cancellationToken);
            builds = builds.Concat(await GetBuildsFromTFSByBranchAsync(buildClient, definitions, "refs/heads/" + Options.RoslynBranchName, resultFilter, cancellationToken));
            return builds;
        }

        private static async Task<List<Build>> GetBuildsFromTFSByBranchAsync(BuildHttpClient buildClient, List<BuildDefinitionReference> definitions, string branchName, BuildResult? resultFilter, CancellationToken cancellationToken)
        {
            return await buildClient.GetBuildsAsync(
                                    project: Options.TFSProjectName,
                                    definitions: definitions.Select(d => d.Id),
                                    branchName: branchName,
                                    statusFilter: BuildStatus.Completed,
                                    resultFilter: resultFilter,
                                    cancellationToken: cancellationToken);
        }

        private static async Task<Build> GetLatestBuildAsync(CancellationToken cancellationToken)
        {
            // ********************* Verify Build Passed *****************************
            cancellationToken.ThrowIfCancellationRequested();
            Build newestBuild = null;
            Log.Info($"Get Latest Passed Build");
            try
            {
                newestBuild = await GetLatestPassedBuildAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new IOException($"Unable to get latest build for '{Options.RoslynBuildQueueName}' from project '{Options.TFSProjectName}' in '{Options.VSTSUri}': {ex.Message}");
            }

            if (newestBuild == null)
            {
                throw new IOException($"Unable to get latest build for '{Options.RoslynBuildQueueName}' from project '{Options.TFSProjectName}' in '{Options.VSTSUri}'");
            }

            // ********************* Get New Build Version****************************
            cancellationToken.ThrowIfCancellationRequested();
            return newestBuild;
        }

        private static async Task<Build> QueueValidationBuildAsync(string branchName)
        {
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();

            // There may be "draft" copies of changes to the build we care about hanging around.
            // We only want the one that is currently deployed (that is, with DefinitionQuality.Definition).
            var definitionReference = (await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.ValidationBuildQueueName))
                .Where(def => def.DefinitionQuality == DefinitionQuality.Definition)
                .Single();
            var definition = await buildClient.GetDefinitionAsync(project: Options.TFSProjectName, definitionId: definitionReference.Id);

            var parameters = CreateValidationBuildParameters(definition.Variables);

            return await buildClient.QueueBuildAsync(
                new Build()
                {
                    Definition = definitionReference,
                    SourceBranch = branchName,
                    Parameters = parameters,
                    Project = definitionReference.Project
                });
        }

        /// <remarks>
        /// Overridable build parameters have default values. However, you either need to provide
        /// all or none of these parameters. If you only provide some, the others won't be defined
        /// at all--even though they have default values.
        /// Here we take the set of build variables and extract out the names and defaults for the
        /// overridable ones, change the values for a couple we care about, and then render them
        /// back to a JSON object that we can pass to the VSO API for creating new builds.
        /// </remarks>
        private static string CreateValidationBuildParameters(IDictionary<string, BuildDefinitionVariable> variables)
        {
            string GetVariableValue(KeyValuePair<string, BuildDefinitionVariable> variable)
            {
                switch (variable.Key)
                {
                    case "EnableDDRITS":
                        return Options.RunDDRITsInValidation.ToString();
                    case "EnableRPS":
                        return Options.RunRPSInValidation.ToString();
                    default:
                        return variable.Value.Value;
                }
            }

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter) { QuoteName = true, CloseOutput = false })
            {
                jsonWriter.WriteStartObject();

                foreach (var variable in variables.Where(v => v.Value.AllowOverride))
                {
                    jsonWriter.WritePropertyName(variable.Key);
                    jsonWriter.WriteValue(GetVariableValue(variable));
                }

                jsonWriter.WriteEndObject();

                return stringWriter.ToString();
            }
        }

        private static async Task<GitPullRequestCommentThread> CreateGitPullRequestCommentThread(int pullRequestId, string commentContent)
        {
            var gitClient = ProjectCollection.GetClient<GitHttpClient>();
            return await gitClient.CreateThreadAsync(new GitPullRequestCommentThread
                {
                    Comments = new List<GitPullRequestComment>
                                   {
                                       new GitPullRequestComment()
                                       {
                                           CommentType = GitPullRequestCommentType.Text,
                                           Content = commentContent
                                       }
                                   }
                },
                project: Options.TFSProjectName,
                repositoryId: "VS",
                pullRequestId: pullRequestId);
        }

        private static async Task<Build> GetSpecificBuildAsync(BuildVersion version, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Log.Trace($"Getting latest passing build from {Options.TFSProjectName} where name is {Options.RoslynBuildQueueName}");
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();
            var definitions = await buildClient.GetDefinitionsAsync(project: Options.TFSProjectName, name: Options.RoslynBuildQueueName);
            var builds = await GetBuildsFromTFSAsync(buildClient, definitions, cancellationToken);
            return (from build in builds
                    where version == BuildVersion.FromTfsBuildNumber(build.BuildNumber, Options.RoslynBuildQueueName)
                    orderby build.FinishTime descending
                    select build).FirstOrDefault();
        }

        private static async Task<Component[]> GetLatestComponentsAsync(Build newestBuild, CancellationToken cancellationToken)
        {
            var logText = await GetLogTextAsync(newestBuild, cancellationToken);
            var urls = GetUrls(logText);
            return GetComponents(urls);
        }

        private static Component[] GetComponents(string[] urls)
        {
            if (urls == null || urls.Length == 0)
            {
                Log.Warn("GetComponents: No URLs specified.");
                return Array.Empty<Component>();
            }

            var result = new Component[urls.Length];
            for (var i = 0; i < urls.Length; i++)
            {
                var urlString = urls[i];

                Uri uri;
                try
                {
                    uri = new Uri(urlString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Exception thrown creating Uri from {urlString}");
                    throw;
                }

                var fileName = urlString.Split(';').Last();
                var name = fileName.Remove(fileName.Length - 6, 6);
                result[i] = new Component(name, fileName, uri);
            }

            return result;
        }

        private static string[] GetUrls(string logText)
        {
            const string startingString = "Manifest Url(s):";
            var manifestStart = logText.IndexOf(startingString);
            if (manifestStart == -1)
            {
                throw new Exception($"Could not locate string '{startingString}'");
            }

            // We're looking for URLs in the form of:
            // https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/dotnet/roslyn/dev15-rc2/20161122.1;PortableFacades.vsman
            const string pattern = @"https://.*vsman\r?$";
            var regex = new Regex(pattern, RegexOptions.Multiline);
            var input = logText.Substring(manifestStart);
            var matches = regex.Matches(input);

            if (matches.Count == 0)
            {
                throw new Exception($"No URLs found.");
            }

            var urls = new string[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                urls[i] = matches[i].Value.Trim();
            }

            foreach (var url in urls)
            {
                Log.Info($"Manifest URL: {url}");
            }

            return urls;
        }

        private static async Task<string> GetLogTextAsync(Build newestBuild, CancellationToken cancellationToken)
        {
            var buildClient = ProjectCollection.GetClient<BuildHttpClient>();

            // Allocate temporary files
            var tempZipFile = Path.GetTempFileName();
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // Get zip file from TFS and write to disk
            using (var logStream = await buildClient.GetBuildLogsZipAsync(Options.TFSProjectName, newestBuild.Id, cancellationToken: cancellationToken))
            using (var tempStream = File.OpenWrite(tempZipFile))
            {
                await logStream.CopyToAsync(tempStream);
            }

            // Open zip file and extract log entry on disk
            using (var zipFile = ZipFile.OpenRead(tempZipFile))
            {
                var entry = zipFile.Entries.SingleOrDefault(x => x.FullName.Contains("Upload VSTS Drop"));
                entry.ExtractToFile(tempFile);
            }

            // Read in log text
            string logText;
            using (var tempFileStream = File.OpenRead(tempFile))
            using (var streamReader = new StreamReader(tempFileStream))
            {
                logText = await streamReader.ReadToEndAsync();
            }

            // Attempt to delete temporary files
            try
            {
                File.Delete(tempZipFile);
                File.Delete(tempFile);
            }
            catch (Exception)
            {
                // swallow exceptions
            }

            return logText;
        }
    }
}
