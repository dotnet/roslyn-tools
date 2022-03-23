// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Roslyn.Tool.Utilities;
using System.Text.RegularExpressions;

namespace Microsoft.Roslyn.Tool.PRFinder;

internal class PRFinder
{
    private static readonly Regex IsGitHubReleaseFlowCommit = new(@"^Merge pull request #\d+ from dotnet/merges/");
    private static readonly Regex IsGitHubMergePRCommit = new(@"^Merge pull request #(\d+) from");
    private static readonly Regex IsGitHubSquashedPRCommit = new(@"\(#(\d+)\)(?:\n|$)");

    public static string GetGitHubPullRequestUrl(string repoURL, string prNumber)
        => $"{repoURL}/pull/{prNumber}";

    const string RepoUrl = @"https://www.github.com/dotnet/roslyn";

    public static async Task<int> FindPRsAsync(string previousCommitSha, string currentCommitSha, ILogger logger)
    {
        var previousCommitExists = (await ProcessRunner.RunProcessAsync("git", $"cat-file -t {previousCommitSha}")).ExitCode == 0;
        var currentCommitExists = (await ProcessRunner.RunProcessAsync("git", $"cat-file -t {currentCommitSha}")).ExitCode == 0;

        if (!previousCommitExists)
        {
            logger.LogError($"Previous commit SHA '{previousCommitSha}' does not exist. Please fetch and try again.");
            return -1;
        }

        if (!currentCommitExists)
        {
            logger.LogError($"Current commit SHA '{currentCommitSha}' does not exist. Please fetch and try again.");
            return -1;
        }

        List<GitCommit> commitLog = new();

        // Get commit history starting at the current commit and ending at the previous commit
        var gitLogResult = await ProcessRunner.RunProcessAsync("git", $"log --date=\"format:%Y-%m-%d %H:%M\" --pretty=\"format:CommitId: %H <<|>>Author: %an <<|>>Committer: %cn <<|>>Subject: %s <<|>>Body: %b <<End>>\" \"{previousCommitSha}..{currentCommitSha}\"");
        if (gitLogResult.ExitCode != 0)
        {
            logger.LogError(gitLogResult.Error);
            return -1;
        }

        foreach (var commitLine in gitLogResult.Output.Split("<<End>>"))
        {
            if (!commitLine.TrimStart().StartsWith("CommitId: "))
            {
                // Continuation of previous commit body text
                continue;
            }

            var parts = commitLine.Split("<<|>>");
            commitLog.Add(new GitCommit
            {
                CommitId = parts[0].Split("CommitId: ")[1].Trim(),
                Author = parts[1].Split("Author: ")[1].Trim(),
                Committer = parts[2].Split("Committer: ")[1].Trim(),
                Subject = parts[3].Split("Subject: ")[1].Trim(),
                Body = parts[4].Split("Body: ")[1].Trim()
            });
        }

        logger.LogInformation($@"Changes since [{previousCommitSha}]({RepoUrl}/commit/{previousCommitSha})");

        var commitHeaderAdded = false;
        var mergePRHeaderAdded = false;
        var mergePRFound = false;

        foreach (var commit in commitLog)
        {
            // Once we've found a Merge PR we can exclude commits not committed by GitHub since Merge and Squash commits are committed by GitHub
            if (commit.Committer != "GitHub" && mergePRFound)
            {
                continue;
            }

            // Exclude arcade dependency updates
            if (commit.Author == "dotnet-maestro[bot]")
            {
                mergePRFound = true;
                continue;
            }

            // Exclude merge commits from auto code-flow PRs (e.g. merges/main-to-main-vs-deps)
            if (IsGitHubReleaseFlowCommit.Match(commit.Subject).Success)
            {
                mergePRFound = true;
                continue;
            }

            string comment = string.Empty;
            string prNumber = string.Empty;

            var match = IsGitHubMergePRCommit.Match(commit.Subject);
            if (match.Success)
            {
                prNumber = match.Groups[1].Value;

                // Merge PR Messages are in the form "Merge pull request #39526 from mavasani/GetValueUsageInfoAssert\n\nFix an assert in IOperationExtension.GetValueUsageInfo"
                // Try and extract the 3rd line since it is the useful part of the message, otherwise take the first line.
                comment = !string.IsNullOrEmpty(commit.Body)
                    ? $"{commit.Body} ({prNumber})"
                    : commit.Subject;
            }
            else
            {
                match = IsGitHubSquashedPRCommit.Match(commit.Subject);
                if (match.Success)
                {
                    prNumber = match.Groups[1].Value;

                    // Squash PR Messages are in the form "Nullable annotate TypeCompilationState and MessageID (#39449)"
                    // Take the 1st line since it should be descriptive.
                    comment = commit.Subject;
                }
            }

            // We will print commit comments until we find the first merge PR
            if (!match.Success && mergePRFound)
            {
                continue;
            }

            string prLink;

            if (match.Success)
            {
                if (commitHeaderAdded && !mergePRHeaderAdded)
                {
                    mergePRHeaderAdded = true;
                    logger.LogInformation("### Merged PRs:");
                }

                mergePRFound = true;

                // Replace "#{prNumber}" with "{prNumber}" so that AzDO won't linkify it
                comment = comment.Replace($"#{prNumber}", prNumber);

                prLink = $@"- [{comment}]({GetGitHubPullRequestUrl(RepoUrl, prNumber)})";
            }
            else
            {
                if (!commitHeaderAdded)
                {
                    commitHeaderAdded = true;
                    logger.LogInformation("### Commits since last PR:");
                }

                var fullSHA = commit.CommitId;
                var shortSHA = fullSHA.Substring(0, 7);

                // Take the subject line since it should be descriptive.
                comment = $"{commit.Subject} ({shortSHA})";

                prLink = $@"- [{comment}]({RepoUrl}/commit/{fullSHA})";
            }

            logger.LogInformation(prLink);
        }

        return 0;
    }

    internal struct GitCommit
    {
        public string CommitId { get; set; }
        public string Author { get; set; }
        public string Committer { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
