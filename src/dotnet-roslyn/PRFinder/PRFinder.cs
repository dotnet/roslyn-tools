// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Roslyn.Tool.PRFinder
{
    internal class PRFinder
    {
        static readonly Regex IsMergePRCommit = new Regex(@"^Merge pull request #(\d+) from");
        static readonly Regex IsSquashedPRCommit = new Regex(@"\(#(\d+)\)$");

        const string RepoPRUrl = @"https://www.github.com/dotnet/roslyn";

        public static int FindPRs(string previousCommitSha, string currentCommitSha)
        {
            using (var repo = new Repository(Environment.CurrentDirectory))
            {
                var currentCommit = repo.Lookup<Commit>(currentCommitSha);
                var previousCommit = repo.Lookup<Commit>(previousCommitSha);

                if (currentCommit is null || previousCommit is null)
                {
                    Console.WriteLine($"Couldn't find commit {(currentCommit is null ? currentCommitSha : previousCommitSha)}");
                    Console.WriteLine("Fetching and trying again...");

                    // it doesn't please me to do this, but libgit2sharp doesn't support ssh easily
                    Process.Start("git", "fetch --all").WaitForExit();
                    Console.WriteLine("--- end of git output ---");
                    Console.WriteLine();

                    currentCommit = repo.Lookup<Commit>(currentCommitSha);
                    previousCommit = repo.Lookup<Commit>(previousCommitSha);
                }

                // Get commit history starting at the current commit and ending at the previous commit
                var commitLog = repo.Commits.QueryBy(
                    new CommitFilter
                    {
                        IncludeReachableFrom = currentCommit,
                        ExcludeReachableFrom = previousCommit
                    });

                Console.WriteLine($@"Changes since [{previousCommitSha}]({RepoPRUrl}/commit/{previousCommitSha})");

                foreach (var commit in commitLog)
                {
                    // Exclude auto-merges
                    if (commit.Author.Name == "dotnet-automerge-bot")
                    {
                        continue;
                    }

                    var match = IsMergePRCommit.Match(commit.MessageShort);

                    if (!match.Success)
                    {
                        match = IsSquashedPRCommit.Match(commit.MessageShort);
                    }

                    if (!match.Success)
                    {
                        continue;
                    }

                    var prNumber = match.Groups[1].Value;
                    var prLink = $@"- [{commit.MessageShort}]({RepoPRUrl}/pull/{prNumber})";

                    Console.WriteLine(prLink);
                }
            }

            return 0;
        }
    }
}
