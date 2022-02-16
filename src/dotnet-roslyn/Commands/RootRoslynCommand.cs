// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;

namespace Microsoft.Roslyn.Tool.Commands
{
    internal static class RootRoslynCommand
    {
        public static RootCommand GetRootCommand() => new RootCommand()
        {
            PRFinderCommand.GetCommand()
        };
    }
}
