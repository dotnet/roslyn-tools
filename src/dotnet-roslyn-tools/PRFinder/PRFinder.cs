// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder;

using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;

internal class PRFinder
{
    public const string DefaultFormat = "default";
    public const string OmniSharpFormat = "o#";
    public const string ChangelogFormat = "changelog";

    /// <summary>
    /// Finds the PRs merged between two given commits.
    /// </summary>
    /// <param name="startRef">Previous commit SHA.</param>
    /// <param name="endRef">Current commit SHA.</param>
    /// <param name="logger">Logger where result will be output.</param>
    /// <param name="repoPath">Optional path to product repo. Current directory will be used otherwise.</param>
    /// <param name="builder">Optional if the caller wants result as a string.</param>
    /// <returns></returns>
    public static async Task<int> FindPRsAsync(
        string startRef,
        string endRef,
        string? path,
        string format,
        ILogger logger,
        string? repoPath = null,
        StringBuilder? builder = null)
    {
        // If we are not provided a repo path, walk up the file system to fine one.
        if (repoPath is null && !TryFindGitRepoPath(Environment.CurrentDirectory, out repoPath))
        {
            logger.LogError("The current directory is not part of a git repository.");
            return -1;
        }

        using var repo = new Repository(repoPath);

        var startCommit = repo.Lookup<Commit>(endRef);
        var endCommit = repo.Lookup<Commit>(startRef);

        if (endCommit is null)
        {
            logger.LogError($"Starting ref '{startRef}' does not exist. Please fetch and try again.");
            return -1;
        }

        if (startCommit is null)
        {
            logger.LogError($"Ending ref '{endRef}' does not exist. Please fetch and try again.");
            return -1;
        }

        var remotes = repo.Network.Remotes.ToDictionary(remote => remote.Name);
        if (!remotes.TryGetValue("upstream", out var remote)
            && !remotes.TryGetValue("origin", out remote))
        {
            remote = remotes.Values.FirstOrDefault();
        }

        if (remote is null)
        {
            logger.LogError("No configured remote for this repository.");
            return 1;
        }

        string repoUrl = string.Empty;
        if (remote.Url.StartsWith("https://")) // https://github.com/dotnet/roslyn.git
        {
            repoUrl = remote.Url.EndsWith(".git")
                ? remote.Url[..^4]
                : remote.Url;
        }
        else if (remote.Url.StartsWith("git@")) // git@github.com:dotnet/roslyn.git
        {
            var colonIndex = remote.Url.IndexOf(':');
            repoUrl = remote.Url.EndsWith(".git")
                ? $"https://{remote.Url[4..colonIndex]}/{remote.Url[++colonIndex..^4]}"
                : $"https://{remote.Url[4..colonIndex]}/{remote.Url[++colonIndex..]}";
        }
        else
        {
            logger.LogError($"Remote '{remote.Name}' has an unsupported Url format '{remote.Url}'.");
            return 1;
        }

        var isGitHub = repoUrl.Contains("github.com");
        var isAzure = repoUrl.Contains("azure.com");
        if (!isGitHub && !isAzure)
        {
            logger.LogError($"Remote '{remote.Name}' has an unsupported URL host '{remote.Url}'.");
            return 1;
        }

        IRepositoryHost host = isGitHub
            ? new Hosts.GitHub(repoUrl, logger)
            : new Hosts.Azure(repoUrl);

        IPRLogFormatter formatter = format switch
        {
            DefaultFormat => new Formatters.DefaultFormatter(),
            OmniSharpFormat => new Formatters.OmniSharpFormatter(),
            ChangelogFormat => new Formatters.ChangelogFormatter(),
            _ => throw new InvalidOperationException($"Uknown format '{format}'")
        };

        // Get commit history starting at the current commit and ending at the previous commit
        path = CleanPath(path);
        var commitFilter = new CommitFilter
        {
            IncludeReachableFrom = startCommit,
            ExcludeReachableFrom = endCommit,
        };
        var commitsForPath = path is not null
            ? repo.Commits.QueryBy(path, commitFilter).Select(e => e.Commit.Sha).ToHashSet()
            : null;
        var commitLog = repo.Commits.QueryBy(commitFilter);

        logger.LogDebug(formatter.FormatChangesHeader(startRef, host.GetCommitUrl(startRef), endRef, host.GetCommitUrl(endRef), path));

        RecordLine(formatter.FormatDiffHeader(host.GetDiffUrl(startRef, endRef)), logger, builder);

        var commitHeaderAdded = false;
        var mergePRHeaderAdded = false;
        var mergePRFound = false;

        foreach (var commit in commitLog)
        {
            if (host.ShouldSkip(commit, ref mergePRFound))
            {
                continue;
            }

            var mergeInfo = await host.TryParseMergeInfoAsync(commit);

            // We will print commit comments until we find the first merge PR
            if (mergeInfo is null && mergePRFound)
            {
                continue;
            }

            // We need to ensure the commit is for the path if one is provided
            if (commitsForPath is not null && !IsCommitForPath(commit, commitsForPath))
            {
                continue;
            }

            string prLink;

            if (mergeInfo is not null)
            {
                if (commitHeaderAdded && !mergePRHeaderAdded)
                {
                    mergePRHeaderAdded = true;
                    RecordLine(formatter.GetPRSectionHeader(), logger, builder);
                }

                mergePRFound = true;

                prLink = formatter.FormatPRListItem(mergeInfo.Comment, mergeInfo.PrNumber, host.GetPullRequestUrl(mergeInfo.PrNumber));
            }
            else
            {
                if (!commitHeaderAdded)
                {
                    commitHeaderAdded = true;
                    RecordLine(formatter.GetCommitSectionHeader(), logger, builder);
                }

                var fullSHA = commit.Sha;
                var shortSHA = fullSHA.Substring(0, 7);

                prLink = formatter.FormatCommitListItem(commit.MessageShort, shortSHA, host.GetCommitUrl(fullSHA));
            }

            RecordLine(prLink, logger, builder);
        }

        return 0;
    }

    private static void RecordLine(string line, ILogger logger, StringBuilder? builder)
    {
        logger.LogInformation(line);
        builder?.AppendLine(line);
    }

    private static bool IsCommitForPath(Commit commit, HashSet<string> commitsForPath)
    {
        // Check if the commit is for the path.
        var commitParents = commit.Parents.ToArray();
        if (commitsForPath.Contains(commit.Sha))
        {
            return true;
        }

        // If the commit isn't a merge commit, then it cannot be for the path.
        if (commitParents.Length == 1)
        {
            return false;
        }

        // Since we report PRs based on merge commits we will walk the parents
        // looking for commit to the path. If we encouter another merge commit
        // we will assume we have gone too far and stop.
        foreach (var parentCommit in commitParents)
        {
            var prCommit = parentCommit;

            while (prCommit?.Parents.Count() == 1)
            {
                if (commitsForPath.Contains(prCommit.Sha))
                {
                    return true;
                }

                prCommit = prCommit.Parents.FirstOrDefault();
            }
        }

        return false;
    }


    private static bool TryFindGitRepoPath(string startPath, [NotNullWhen(returnValue: true)] out string? repoPath)
    {
        var currentDirectory = new DirectoryInfo(startPath);
        while (currentDirectory != null)
        {
            if (currentDirectory.GetDirectories(".git").Length != 0)
            {
                repoPath = currentDirectory.FullName;
                return true;
            }

            currentDirectory = currentDirectory.Parent;
        }

        repoPath = null;
        return false;
    }

    private static string? CleanPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Clean up the path
        path = path.Replace('\\', '/');
        if (path.StartsWith("./"))
        {
            path = path[2..];
        }

        return path;
    }
}
