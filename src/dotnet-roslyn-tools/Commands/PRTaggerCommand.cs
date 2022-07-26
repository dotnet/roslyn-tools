// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Commands;

using System.CommandLine.Invocation;
using System.CommandLine;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();

    internal static readonly Option<string> VSBuild = new(new[] { "--build", "-b" }, "VS build number") { IsRequired = true };
    internal static readonly Option<string> CommitId = new(new[] { "--commit", "-c" }, "VS build commit") { IsRequired = true };
    internal static readonly Option<string> GitHubUsername = new(new[] { "--username", "-u" }, "GitHub username") { IsRequired = true };
    internal static readonly Option<string> GitHubPassword = new(new[] { "--password", "-p" }, "GitHub password") { IsRequired = true };

    public static Symbol GetCommand()
    {
        var command = new Command("pr-tagger", "Tags PRs inserted in a given VS build.")
        {
            VSBuild,
            CommitId,
            GitHubUsername,
            GitHubPassword,
            CommonOptions.DevDivAzDOTokenOption,
        };

        command.Handler = s_prTaggerCommandHandler;
        return command;
    }

    private class PRTaggerCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var vsBuild = context.ParseResult.GetValueForOption(VSBuild)!;
            var vsCommitSha = context.ParseResult.GetValueForOption(CommitId)!;
            var gitHubUsername = context.ParseResult.GetValueForOption(GitHubUsername)!;
            var gitHubPassword = context.ParseResult.GetValueForOption(GitHubPassword)!;
            var settings = context.ParseResult.LoadSettings(logger);

            return await PRTagger.PRTagger.TagPRs(
                vsBuild, vsCommitSha, settings, gitHubUsername, gitHubPassword, logger, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
