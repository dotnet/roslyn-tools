// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;

namespace Microsoft.Roslyn.Tool.Commands
{
    internal static class PRFinderCommand
    {
        private static readonly PrFinderCommandDefaultHandler s_prFinderCommandHandler = new();

        internal static readonly Option<string> PreviousCommitShaOption = new Option<string>(new[] { "--previous", "-p" }, "SHA of the commit you want to start looking for PRs from") { IsRequired = true };
        internal static readonly Option<string> CurrentCommitSHAOption = new Option<string>(new[] { "--current", "-c" }, "SHA of commit you want to stop looking for PRs at") { IsRequired = true };

        public static Symbol GetCommand()
        {
            var prFinderCommand = new Command("pr-finder", "Find PRs between two commits")
            {
                PreviousCommitShaOption,
                CurrentCommitSHAOption
            };
            prFinderCommand.Handler = s_prFinderCommandHandler;
            return prFinderCommand;
        }

        private class PrFinderCommandDefaultHandler : ICommandHandler
        {
            public Task<int> InvokeAsync(InvocationContext context)
            {
                var previousCommit = context.ParseResult.GetValueForOption(PreviousCommitShaOption)!;
                var currentCommit = context.ParseResult.GetValueForOption(CurrentCommitSHAOption)!;

                return PRFinder.PRFinder.FindPRsAsync(previousCommit, currentCommit);
            }
        }
    }
}
