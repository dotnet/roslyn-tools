// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Commands;

using System.CommandLine.Invocation;
using System.CommandLine;
using Microsoft.RoslynTools.Authentication;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();

    private static readonly Option<string> ProductName = new(new[] { "--product-name", "-p" }, "Name of product (e.g. 'roslyn' or 'razor')") { IsRequired = true };
    private static readonly Option<string> VSBuild = new(new[] { "--build", "-b" }, "VS build number") { IsRequired = true };
    private static readonly Option<string> CommitId = new(new[] { "--commit", "-c" }, "VS build commit") { IsRequired = true };
    private static readonly Option<string> GitHubToken = new(new[] { "--github-token" }, "Token used to authenticate GitHub.") { IsRequired = true };
    private static readonly Option<string> DevDivAzDOToken = new(new[] { "--devdiv-azdo-token" }, "Token used to authenticate to DevDiv Azure DevOps.") { IsRequired = true };
    private static readonly Option<string> DncEngAzDOToken = new(new[] { "--dnceng-azdo-token" }, "Token used to authenticate to DncEng Azure DevOps.") { IsRequired = true };

    public static Symbol GetCommand()
    {
        var command = new Command("pr-tagger", "Tags PRs inserted in a given VS build.")
        {
            ProductName,
            VSBuild,
            CommitId,
            GitHubToken,
            DevDivAzDOToken,
            DncEngAzDOToken,
        };

        command.Handler = s_prTaggerCommandHandler;
        return command;
    }

    private class PRTaggerCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var productName = context.ParseResult.GetValueForOption(ProductName)!;
            var vsBuild = context.ParseResult.GetValueForOption(VSBuild)!;
            var vsCommitSha = context.ParseResult.GetValueForOption(CommitId)!;
            var settings = new RoslynToolsSettings
            {
                GitHubToken = context.ParseResult.GetValueForOption(GitHubToken)!,
                DevDivAzureDevOpsToken = context.ParseResult.GetValueForOption(DevDivAzDOToken)!,
                DncEngAzureDevOpsToken = context.ParseResult.GetValueForOption(DncEngAzDOToken)!
            };

            return await PRTagger.PRTagger.TagPRs(
                productName, vsBuild, vsCommitSha, settings, logger, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
