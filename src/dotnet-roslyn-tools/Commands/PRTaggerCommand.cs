// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Commands;

using System.CommandLine.Invocation;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Products;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

internal static class PRTaggerCommand
{
    private static readonly PRTaggerCommandDefaultHandler s_prTaggerCommandHandler = new();

    private static readonly Roslyn s_roslynInfo = new();
    private static readonly Razor s_razorInfo = new();
    private static readonly FSharp s_fsharpInfo = new();

    private static readonly Option<string> ProductName = new Option<string>(new[] { "--product-name", "-n" }, "Optional name of product (e.g. 'roslyn' or 'razor'). If provided, can skip providing github-repo-url, component-json-file-name, component-name, and build-pipeline-name.")
        .FromAmong("roslyn", "razor", "fsharp");
    private static readonly Option<string> GitHubRepoUrl = new(new[] { "--github-repo-url", "-u" }, "GitHub repo URL");
    private static readonly Option<string> ComponentJsonFileName = new(new[] { "--component-json-file-name" }, @"Name of component JSON file (e.g. '.corext\Configs\dotnetcodeanalysis-components.json'");
    private static readonly Option<string> ComponentName = new(new[] { "--component-name" }, @"Name of component within JSON file (e.g. 'Microsoft.CodeAnalysis.LanguageService')'");
    private static readonly Option<string> BuildPipelineName = new(new[] { "--build-pipeline-name" }, "Name of build pipeline (e.g. 'dotnet-roslyn CI')'");

    private static readonly Option<string> VSBuild = new(new[] { "--build", "-b" }, "VS build number") { IsRequired = true };
    private static readonly Option<string> CommitId = new(new[] { "--commit", "-c" }, "VS build commit") { IsRequired = true };

    public static Symbol GetCommand()
    {
        var command = new Command("pr-tagger", "Tags PRs inserted in a given VS build.")
        {
            ProductName,
            GitHubRepoUrl,
            ComponentJsonFileName,
            ComponentName,
            BuildPipelineName,
            VSBuild,
            CommitId,
            CommonOptions.GitHubTokenOption,
            CommonOptions.DevDivAzDOTokenOption,
            CommonOptions.DncEngAzDOTokenOption
        };

        command.Handler = s_prTaggerCommandHandler;
        return command;
    }

    private class PRTaggerCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();

            var gitHubRepoUrl = context.ParseResult.GetValueForOption(GitHubRepoUrl);
            var componentJsonFileName = context.ParseResult.GetValueForOption(ComponentJsonFileName);
            var componentName = context.ParseResult.GetValueForOption(ComponentName);
            var buildPipelineName = context.ParseResult.GetValueForOption(BuildPipelineName);

            // If product name is provided, we can skip parsing some arguments.
            var productName = context.ParseResult.GetValueForOption(ProductName);
            if (productName is not null)
            {
                if (!TryGetProductInfo(productName, out var productInfo))
                {
                    logger.LogError("Error retrieving product info.");
                    return -1;
                }

                gitHubRepoUrl = productInfo.RepoBaseUrl;
                componentJsonFileName = productInfo.ComponentJsonFileName;
                componentName = productInfo.ComponentName;
                buildPipelineName = productInfo.GetBuildPipelineName("internal") ?? productInfo.GetBuildPipelineName("DevDiv");
            }

            if (gitHubRepoUrl is null)
            {
                logger.LogError("Null GitHub repo URL.");
                return -1;
            }

            if (componentJsonFileName is null)
            {
                logger.LogError("Null component JSON file name.");
                return -1;
            }

            if (componentName is null)
            {
                logger.LogError("Null component name.");
                return -1;
            }

            if (buildPipelineName is null)
            {
                logger.LogError("Null build pipeline name.");
                return -1;
            }

            var vsBuild = context.ParseResult.GetValueForOption(VSBuild)!;
            var vsCommitSha = context.ParseResult.GetValueForOption(CommitId)!;
            var settings = context.ParseResult.LoadSettings(logger);

            if (string.IsNullOrEmpty(settings.GitHubToken) ||
                string.IsNullOrEmpty(settings.DevDivAzureDevOpsToken) ||
                string.IsNullOrEmpty(settings.DncEngAzureDevOpsToken))
            {
                logger.LogError("Missing authentication token.");
                return -1;
            }

            return await PRTagger.PRTagger.TagPRs(
                gitHubRepoUrl,
                componentJsonFileName,
                componentName,
                buildPipelineName,
                vsBuild,
                vsCommitSha,
                settings,
                logger,
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static bool TryGetProductInfo(string productName, [NotNullWhen(true)] out IProduct? productInfo)
    {
        if (productName.Equals(s_roslynInfo.Name, StringComparison.OrdinalIgnoreCase))
        {
            productInfo = s_roslynInfo;
            return true;
        }
        else if (productName.Equals(s_razorInfo.Name, StringComparison.OrdinalIgnoreCase))
        {
            productInfo = s_razorInfo;
            return true;
        }
        else if (productName.Equals(s_fsharpInfo.Name, StringComparison.OrdinalIgnoreCase))
        {
            productInfo = s_fsharpInfo;
            return true;
        }

        productInfo = null;
        return false;
    }
}
