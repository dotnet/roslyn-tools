using Octokit;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubCodeReviewDashboard
{
    public static class Dashboard
    {
        public static async Task<ImmutableDictionary<string, ImmutableArray<PullRequest>>> GetCategorizedPullRequests()
        {
            var github = new GitHubClient(new ProductHeaderValue("dotnet-roslyn-Code-Review-Dashboard"));
            github.Credentials = new Credentials(Startup.GitHubToken);
            var openPullRequests = await GetAllPullRequests(github);
            var ideTeamMembers = (await github.Organization.Team.GetAllMembers(1781706)).Select(u => u.Login).ToList();

            var pullRequestsByCategory = new Dictionary<string, ImmutableArray<PullRequest>.Builder>();

            void AddToCategory(string category, PullRequest pullRequest)
            {
                if (!pullRequestsByCategory.TryGetValue(category, out var pullRequests))
                {
                    pullRequests = ImmutableArray.CreateBuilder<PullRequest>();
                    pullRequestsByCategory.Add(category, pullRequests);
                }

                pullRequests.Add(pullRequest);
            }

            foreach (var openPullRequest in openPullRequests)
            {
                var requestedReviewers = openPullRequest.RequestedReviewers.Where(a => ideTeamMembers.Contains(a.Login))
                                                                           .Select(a => a.Login).ToList();

                // For assignees, exclude self-assignment since that's not terribly useful to show.
                var assignees = openPullRequest.Assignees.Where(a => ideTeamMembers.Contains(a.Login) &&
                                                                     a.Id != openPullRequest.User.Id)
                                                         .Select(a => a.Login).ToList();

                // We will exclude PRs created by dotnet-bot since those are usually auto-merges, but if it's
                // assigned to somebody then we'll still show that since that might mean the merge resolution needs
                // something done with it.
                if (openPullRequest.RequestedTeams.Any(t => t.Name == "roslyn-ide") &&
                    !requestedReviewers.Any() &&
                    !assignees.Any() &&
                    !openPullRequest.Draft &&
                    openPullRequest.User.Login != "dotnet-bot")
                {
                    AddToCategory("(untriaged)", openPullRequest);
                }
                else
                {
                    // If the PR is a draft PR, we'll only show explicit requests, otherwise requests plus assignees
                    // since for community members people we assign the PR to are on the hook for reviewing as well.
                    var responsibleUsers = openPullRequest.Draft ? requestedReviewers : requestedReviewers.Concat(assignees).Distinct();

                    foreach (var responsibleUser in responsibleUsers)
                    {
                        AddToCategory(responsibleUser, openPullRequest);
                    }
                }
            }

            return ImmutableDictionary.CreateRange(
                pullRequestsByCategory.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToImmutable())));
        }

        private static async Task<ImmutableArray<PullRequest>> GetAllPullRequests(GitHubClient github)
        {
            (string org, string name)[] repositories = [
                ("dotnet", "format"),
                ("dotnet", "roslyn"),
                ("dotnet", "roslyn-analyzers"),
                ("dotnet", "roslyn-sdk"),
                ("dotnet", "roslyn-tools"),
                ("dotnet", "vscode-csharp"),
            ];

            var tasks = repositories.Select(repo => github.PullRequest.GetAllForRepository(repo.org, repo.name, new ApiOptions { PageSize = 100 }));
            var allPullRequests = await Task.WhenAll(tasks);

            return allPullRequests.SelectMany(prs => prs).OrderByDescending(pr => pr.CreatedAt).ToImmutableArray();
        }
    }
}
