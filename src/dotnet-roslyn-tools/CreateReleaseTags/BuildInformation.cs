﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.CreateReleaseTags;

internal sealed class BuildInformation
{
    public readonly string CommitSha;
    public readonly string SourceBranch;
    public readonly string BuildId;

    public BuildInformation(string commitSha, string sourceBranch, string buildId)
    {
        CommitSha = commitSha;
        SourceBranch = sourceBranch;
        BuildId = buildId;
    }
}
