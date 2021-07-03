// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;

#if !NETCOREAPP
namespace System.Runtime.CompilerServices
{
    class IsExternalInit { }
}
#endif

namespace Roslyn.Insertion
{
    public record RoslynInsertionToolOptions(
        string VisualStudioRepoAzdoUsername,
        string VisualStudioRepoAzdoUri,
        string VisualStudioRepoProjectName,
        string ComponentBuildQueueName,
        string BuildConfig,
        string InsertionBranchName,
        string BuildDropPath,
        bool InsertCoreXTPackages,
        bool UpdateCoreXTLibraries,
        bool InsertDevDivSourceFiles,
        bool InsertWillowPackages,
        string InsertionName,
        bool RetainInsertedBuild,
        bool QueueValidationBuild,
        string ValidationBuildQueueName,
        bool RunDDRITsInValidation,
        bool RunRPSInValidation,
        string LogFileLocation,
        bool CreateDraftPr,
        ImmutableArray<string> SkipCoreXTPackages)
    {

        public string VisualStudioRepoAzdoPassword { get; init; }
        public string VisualStudioBranchName { get; init; }
        public string ComponentBuildAzdoUsername { get; init; }
        public string ComponentBuildAzdoPassword { get; init; }
        public string ComponentBuildAzdoUri { get; init; }
        public string ComponentBuildProjectName { get; init; }
        public string ComponentBranchName { get; init; }
        public string ComponentGitHubRepoName { get; init; }
        public string SpecificBuild { get; init; }
        public bool UpdateAssemblyVersions { get; init; }
        public bool InsertToolset { get; init; }
        public bool CreateDummyPr { get; init; }
        public int UpdateExistingPr { get; init; }
        public bool OverwritePr { get; init; }
        public string ClientId { get; init; }
        public string ClientSecret { get; init; }
        public string TitlePrefix { get; init; }
        public string TitleSuffix { get; init; }
        public bool SetAutoComplete { get; init; }
        public ImmutableArray<string> CherryPick { get; init; }
        public string ReviewerGUID { get; init; }

        public string ComponentBuildProjectNameOrFallback
            => ComponentBuildProjectName ?? VisualStudioRepoProjectName;

        public bool Valid
        {
            get
            {
                if (ComponentBuildAzdoUri != null && (ComponentBuildAzdoUsername is null || ComponentBuildAzdoPassword is null))
                {
                    // When the Build AzDO instance is separate from the VS AzDO instance, separate credentials must be specified.
                    return false;
                }

                if (CreateDummyPr)
                {
                    // only InsertionName and VisualStudioBranchName are required for creating a dummy pr
                    return
                        UpdateExistingPr == 0 &&
                        !OverwritePr &&
                        !string.IsNullOrEmpty(InsertionName) &&
                        !string.IsNullOrEmpty(VisualStudioBranchName);
                }
                else if (UpdateExistingPr != 0)
                {
                    // only the existing pr ID, InsertionName, BranchName, and BuildQueueName are required for overwriting an existing pr
                    return
                        !CreateDummyPr &&
                        (!CreateDraftPr || OverwritePr) && // Create draft PR can only be specified when overwriting an existing pr
                        !string.IsNullOrEmpty(InsertionName) &&
                        !string.IsNullOrEmpty(ComponentBranchName) &&
                        !string.IsNullOrEmpty(VisualStudioBranchName) &&
                        !string.IsNullOrEmpty(ComponentBuildQueueName);
                }
                else
                {
                    return
                        !OverwritePr &&
                        !string.IsNullOrEmpty(VisualStudioRepoAzdoUsername) &&
                        !string.IsNullOrEmpty(VisualStudioRepoAzdoPassword) &&
                        !string.IsNullOrEmpty(VisualStudioBranchName) &&
                        !string.IsNullOrEmpty(ComponentBuildQueueName) &&
                        !string.IsNullOrEmpty(ComponentBranchName) &&
                        !string.IsNullOrEmpty(BuildConfig) &&
                        !string.IsNullOrEmpty(VisualStudioRepoAzdoUri) &&
                        !string.IsNullOrEmpty(VisualStudioRepoProjectName) &&
                        !string.IsNullOrEmpty(BuildDropPath);
                }
            }
        }

        public string ValidationErrors
        {
            get
            {
                var builder = new StringBuilder();

                if (ComponentBuildAzdoUri != VisualStudioRepoAzdoUri)
                {
                    if (ComponentBuildAzdoUsername is null)
                    {
                        builder.AppendLine($"When {nameof(ComponentBuildAzdoUri)} is specified you must also specify the {nameof(ComponentBuildAzdoUsername)}.");
                    }

                    if (ComponentBuildAzdoPassword is null)
                    {
                        builder.AppendLine($"When {nameof(ComponentBuildAzdoUri)} is specified you must also specify the {nameof(ComponentBuildAzdoPassword)}.");
                    }
                }

                if (CreateDummyPr)
                {
                    // only InsertionName and VisualStudioBranchName are required for creating a dummy pr
                    if (UpdateExistingPr != 0)
                    {
                        builder.AppendLine($"{nameof(CreateDummyPr).ToLowerInvariant()} and {nameof(UpdateExistingPr).ToLowerInvariant()} are mutually exclusive and cannot be specified together");
                    }

                    if (OverwritePr)
                    {
                        builder.AppendLine($"{nameof(CreateDummyPr).ToLowerInvariant()} and {nameof(OverwritePr).ToLowerInvariant()} are mutually exclusive and cannot be specified together");
                    }

                    if (string.IsNullOrEmpty(InsertionName))
                    {
                        builder.AppendLine($"{nameof(InsertionName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioBranchName))
                    {
                        builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                    }
                }
                else if (UpdateExistingPr != 0)
                {
                    // perform a regular insertion
                    if (CreateDraftPr && !OverwritePr)
                    {
                        builder.AppendLine($"{nameof(CreateDraftPr).ToLowerInvariant()} can only be used with {nameof(UpdateExistingPr).ToLowerInvariant()} when {nameof(OverwritePr).ToLowerInvariant()} is true.");
                    }

                    // only the existing pr ID, InsertionName, BranchName, and BuildQueueName are required for overwriting an existing pr
                    if (string.IsNullOrEmpty(InsertionName))
                    {
                        builder.AppendLine($"{nameof(InsertionName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(ComponentBranchName))
                    {
                        builder.AppendLine($"{nameof(ComponentBranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioBranchName))
                    {
                        builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(ComponentBuildQueueName))
                    {
                        builder.AppendLine($"{nameof(ComponentBuildQueueName).ToLowerInvariant()} is required");
                    }
                }
                else
                {
                    // perform a regular insertion
                    if (OverwritePr)
                    {
                        builder.AppendLine($"{nameof(OverwritePr).ToLowerInvariant()} can only be used with {nameof(UpdateExistingPr).ToLowerInvariant()}.");
                    }

                    if (string.IsNullOrEmpty(VisualStudioRepoAzdoUsername))
                    {
                        builder.AppendLine($"{nameof(VisualStudioRepoAzdoUsername).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioRepoAzdoPassword))
                    {
                        builder.AppendLine($"{nameof(VisualStudioRepoAzdoPassword).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioBranchName))
                    {
                        builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(ComponentBuildQueueName))
                    {
                        builder.AppendLine($"{nameof(ComponentBuildQueueName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(ComponentBranchName))
                    {
                        builder.AppendLine($"{nameof(ComponentBranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(BuildConfig))
                    {
                        builder.AppendLine($"{nameof(BuildConfig).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioRepoAzdoUri))
                    {
                        builder.AppendLine($"{nameof(VisualStudioRepoAzdoUri).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioRepoProjectName))
                    {
                        builder.AppendLine($"{nameof(VisualStudioRepoProjectName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(BuildDropPath))
                    {
                        builder.AppendLine($"{nameof(BuildDropPath).ToLowerInvariant()} is required");
                    }
                }

                return builder.ToString();
            }
        }
    }
}
