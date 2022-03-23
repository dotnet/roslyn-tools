// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Roslyn.VS;

namespace Microsoft.Roslyn.Tool.Commands;

using static CommonOptions;

internal class VSBranchInfoCommand
{
    private static readonly VSBranchInfoCommandDefaultHandler s_vsBranchInfoCommandHandler = new();

    internal static readonly Option<string> BranchOption = new Option<string>(new[] { "--branch", "-b" }, () => "main", "Which VS branch to show information for (eg main, rel/d17.1)");
    internal static readonly Option<Product> ProductOption = new Option<Product>(new[] { "--product", "-p" }, () => Product.Roslyn, "Which product to get info for (roslyn or razor)");

    public static Symbol GetCommand()
    {
        var command = new Command("vsbranchinfo", "Provides information about the state of Roslyn in one or more branchs of Visual Studio.")
        {
            BranchOption,
            ProductOption,
            VerbosityOption
        };
        command.Handler = s_vsBranchInfoCommandHandler;
        return command;
    }

    private class VSBranchInfoCommandDefaultHandler : ICommandHandler
    {
        public Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();

            var branch = context.ParseResult.GetValueForOption(BranchOption)!;
            var product = context.ParseResult.GetValueForOption(ProductOption)!;

            return VSBranchInfo.GetInfoAsync(branch, product, logger);
        }
    }
}

