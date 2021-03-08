// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Roslyn.Insertion
{
    static partial class RoslynInsertionTool
    {
        private static string GetNewBranchName() => $"{Options.NewBranchName}{Options.VisualStudioBranchName.Split('/').Last()}.{DateTime.Now:yyyyMMddHHmmss}";

        private static async Task<GitPullRequest> CreatePlaceholderBranchAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var gitClient = Connection.GetClient<GitHttpClient>();
            var repository = await gitClient.GetRepositoryAsync(project: Options.TFSProjectName, repositoryId: "VS", cancellationToken: cancellationToken);

            var refs = await gitClient.GetRefsAsync(
                repository.Id,
                filter: $"heads/{Options.VisualStudioBranchName}",
                cancellationToken: cancellationToken);
            GitRef sourceBranch = refs.Single(r => r.Name == $"refs/heads/{Options.VisualStudioBranchName}");

            var branchName = GetNewBranchName();

            _ = await gitClient.CreatePushAsync(new GitPush()
            {
                RefUpdates = new[] {
                    new GitRefUpdate()
                    {
                        Name = $"refs/heads/{branchName}",
                        OldObjectId = sourceBranch.ObjectId,
                    }
                },
                Commits = new[] {
                    new GitCommitRef()
                    {
                        Comment = $"PLACEHOLDER INSERTION FOR {Options.InsertionName}",
                        Changes = new GitChange[]
                        {
                            new GitChange()
                            {
                                ChangeType = VersionControlChangeType.Delete,
                                Item = new GitItem() { Path = "/Init.ps1" }
                            },
                        }
                    },
                },
            }, repository.Id, cancellationToken: cancellationToken);

            return await CreatePullRequestAsync(
                branchName,
                $"PLACEHOLDER INSERTION FOR {Options.InsertionName}",
                "Not Specified",
                Options.TitlePrefix,
                reviewerId: MLInfraSwatUserId.ToString(),
                cancellationToken);
        }
    }
}
