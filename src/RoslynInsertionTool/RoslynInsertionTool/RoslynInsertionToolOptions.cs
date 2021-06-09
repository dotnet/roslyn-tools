// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Roslyn.Insertion
{
    // borrowed from https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Optional.cs
    public readonly struct Optional<T>
    {
        public bool HasValue { get; }
        public T Value { get; }

        public Optional(T value)
        {
            HasValue = true;
            Value = value;
        }

        public T ValueOrFallback(T fallback)
        {
            return HasValue ? Value : fallback;
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }
    }

    public sealed class RoslynInsertionToolOptions
    {
        public RoslynInsertionToolOptions() { }

        private RoslynInsertionToolOptions(
            string visualStudioRepoAzdoUsername,
            string visualStudioRepoAzdoPassword,
            string visualStudioRepoAzdoUri,
            string visualStudioRepoProjectName,
            string visualStudioBranchName,
            string componentBuildAzdoUsername,
            string componentBuildAzdoPassword,
            string componentBuildAzdoUri,
            string componentBuildProjectName,
            string componentBuildQueueName,
            string componentBranchName,
            string componentGitHubRepoName,
            string buildConfig,
            string insertionBranchName,
            string buildDropPath,
            string specificBuild,
            bool insertCoreXTPackages,
            bool updateCoreXTLibraries,
            bool updateAssemblyVersions,
            bool insertDevDivSourceFiles,
            bool insertWillowPackages,
            string insertionName,
            bool insertToolset,
            bool retainInsertedBuild,
            bool queueValidationBuild,
            string validationBuildQueueName,
            bool runDDRITsInValidation,
            bool runRPSInValidation,
            bool createDummyPr,
            int updateExistingPr,
            bool overwritePr,
            string logFileLocation,
            string clientId,
            string clientSecret,
            string titlePrefix,
            bool createDraftPr,
            bool setAutoComplete,
            ImmutableArray<string> cherryPick,
            ImmutableArray<string> skipCoreXTPackages)
        {
            VisualStudioRepoAzdoUsername = visualStudioRepoAzdoUsername;
            VisualStudioRepoAzdoPassword = visualStudioRepoAzdoPassword;
            VisualStudioRepoAzdoUri = visualStudioRepoAzdoUri;
            VisualStudioRepoProjectName = visualStudioRepoProjectName;
            VisualStudioBranchName = visualStudioBranchName;
            ComponentBuildAzdoUsername = componentBuildAzdoUsername;
            ComponentBuildAzdoPassword = componentBuildAzdoPassword;
            ComponentBuildAzdoUri = componentBuildAzdoUri;
            ComponentBuildProjectName = componentBuildProjectName;
            ComponentBuildQueueName = componentBuildQueueName;
            ComponentBranchName = componentBranchName;
            ComponentGitHubRepoName = componentGitHubRepoName;
            BuildConfig = buildConfig;
            InsertionBranchName = insertionBranchName;
            BuildDropPath = buildDropPath;
            SpecificBuild = specificBuild;
            InsertCoreXTPackages = insertCoreXTPackages;
            UpdateCoreXTLibraries = updateCoreXTLibraries;
            UpdateAssemblyVersions = updateAssemblyVersions;
            InsertDevDivSourceFiles = insertDevDivSourceFiles;
            InsertWillowPackages = insertWillowPackages;
            InsertionName = insertionName;
            InsertToolset = insertToolset;
            RetainInsertedBuild = retainInsertedBuild;
            QueueValidationBuild = queueValidationBuild;
            ValidationBuildQueueName = validationBuildQueueName;
            RunDDRITsInValidation = runDDRITsInValidation;
            RunRPSInValidation = runRPSInValidation;
            CreateDummyPr = createDummyPr;
            UpdateExistingPr = updateExistingPr;
            OverwritePr = overwritePr;
            LogFileLocation = logFileLocation;
            ClientId = clientId;
            ClientSecret = clientSecret;
            TitlePrefix = titlePrefix;
            CreateDraftPr = createDraftPr;
            SetAutoComplete = setAutoComplete;
            CherryPick = cherryPick;
            SkipCoreXTPackages = skipCoreXTPackages;
        }

        public RoslynInsertionToolOptions Update(
            Optional<string> visualStudioRepoAzdoUsername = default,
            Optional<string> visualStudioRepoAzdoPassword = default,
            Optional<string> visualStudioRepoAzdoUri = default,
            Optional<string> visualStudioProjectName = default,
            Optional<string> visualStudioBranchName = default,
            Optional<string> componentBuildAzdoUsername = default,
            Optional<string> componentBuildAzdoPassword = default,
            Optional<string> componentBuildAzdoUri = default,
            Optional<string> componentBuildProjectName = default,
            Optional<string> componentBuildQueueName = default,
            Optional<string> componentBranchName = default,
            Optional<string> componentGitHubRepoName = default,
            Optional<string> buildConfig = default,
            Optional<string> insertionBranchName = default,
            Optional<string> buildDropPath = default,
            Optional<string> specificBuild = default,
            Optional<bool> insertCoreXTPackages = default,
            Optional<bool> updateCoreXTLibraries = default,
            Optional<bool> updateAssemblyVersions = default,
            Optional<bool> insertDevDivSourceFiles = default,
            Optional<bool> insertWillowPackages = default,
            Optional<string> insertionName = default,
            Optional<bool> insertToolset = default,
            Optional<bool> retainInsertedBuild = default,
            Optional<bool> queueValidationBuild = default,
            Optional<string> validationBuildQueueName = default,
            Optional<bool> runDDRITsInValidation = default,
            Optional<bool> runRPSInValidation = default,
            Optional<bool> createDummyPr = default,
            Optional<int> updateExistingPr = default,
            Optional<bool> overwritePr = default,
            Optional<string> logFileLocation = default,
            Optional<string> clientId = default,
            Optional<string> clientSecret = default,
            Optional<string> titlePrefix = default,
            Optional<bool> createDraftPr = default,
            Optional<bool> setAutoComplete = default,
            Optional<ImmutableArray<string>> cherryPick = default,
            Optional<ImmutableArray<string>> skipCoreXTPackages = default)
        {
            return new RoslynInsertionToolOptions(
                visualStudioRepoAzdoUsername: visualStudioRepoAzdoUsername.ValueOrFallback(VisualStudioRepoAzdoUsername),
                visualStudioRepoAzdoPassword: visualStudioRepoAzdoPassword.ValueOrFallback(VisualStudioRepoAzdoPassword),
                visualStudioRepoAzdoUri: visualStudioRepoAzdoUri.ValueOrFallback(VisualStudioRepoAzdoUri),
                visualStudioRepoProjectName: visualStudioProjectName.ValueOrFallback(VisualStudioRepoProjectName),
                visualStudioBranchName: visualStudioBranchName.ValueOrFallback(VisualStudioBranchName),
                componentBuildAzdoUsername: componentBuildAzdoUsername.ValueOrFallback(ComponentBuildAzdoUsername),
                componentBuildAzdoPassword: componentBuildAzdoPassword.ValueOrFallback(ComponentBuildAzdoPassword),
                componentBuildAzdoUri: componentBuildAzdoUri.ValueOrFallback(ComponentBuildAzdoUri),
                componentBuildProjectName: componentBuildProjectName.ValueOrFallback(ComponentBuildProjectName),
                componentBuildQueueName: componentBuildQueueName.ValueOrFallback(ComponentBuildQueueName),
                componentBranchName: componentBranchName.ValueOrFallback(ComponentBranchName),
                componentGitHubRepoName: componentGitHubRepoName.ValueOrFallback(ComponentGitHubRepoName),
                buildConfig: buildConfig.ValueOrFallback(BuildConfig),
                insertionBranchName: insertionBranchName.ValueOrFallback(InsertionBranchName),
                buildDropPath: buildDropPath.ValueOrFallback(BuildDropPath),
                specificBuild: specificBuild.ValueOrFallback(SpecificBuild),
                insertCoreXTPackages: insertCoreXTPackages.ValueOrFallback(InsertCoreXTPackages),
                updateCoreXTLibraries: updateCoreXTLibraries.ValueOrFallback(UpdateCoreXTLibraries),
                updateAssemblyVersions: updateAssemblyVersions.ValueOrFallback(UpdateAssemblyVersions),
                insertDevDivSourceFiles: insertDevDivSourceFiles.ValueOrFallback(InsertDevDivSourceFiles),
                insertWillowPackages: insertWillowPackages.ValueOrFallback(InsertWillowPackages),
                insertionName: insertionName.ValueOrFallback(InsertionName),
                insertToolset: insertToolset.ValueOrFallback(InsertToolset),
                retainInsertedBuild: retainInsertedBuild.ValueOrFallback(RetainInsertedBuild),
                queueValidationBuild: queueValidationBuild.ValueOrFallback(QueueValidationBuild),
                validationBuildQueueName: validationBuildQueueName.ValueOrFallback(ValidationBuildQueueName),
                runDDRITsInValidation: runDDRITsInValidation.ValueOrFallback(RunDDRITsInValidation),
                runRPSInValidation: runRPSInValidation.ValueOrFallback(RunRPSInValidation),
                createDummyPr: createDummyPr.ValueOrFallback(CreateDummyPr),
                updateExistingPr: updateExistingPr.ValueOrFallback(UpdateExistingPr),
                overwritePr: overwritePr.ValueOrFallback(OverwritePr),
                logFileLocation: logFileLocation.ValueOrFallback(LogFileLocation),
                clientId: clientId.ValueOrFallback(ClientId),
                clientSecret: clientSecret.ValueOrFallback(ClientSecret),
                titlePrefix: titlePrefix.ValueOrFallback(TitlePrefix),
                createDraftPr: createDraftPr.ValueOrFallback(CreateDraftPr),
                setAutoComplete: setAutoComplete.ValueOrFallback(SetAutoComplete),
                cherryPick: cherryPick.ValueOrFallback(CherryPick),
                skipCoreXTPackages: skipCoreXTPackages.ValueOrFallback(SkipCoreXTPackages));
        }

        public RoslynInsertionToolOptions WithRunRPSInValidation(bool runRPSInValidation) => Update(runRPSInValidation: runRPSInValidation);

        public RoslynInsertionToolOptions WithRunDDRITsInValidation(bool runDDRITsInValidation) => Update(runDDRITsInValidation: runDDRITsInValidation);

        public RoslynInsertionToolOptions WithValidationBuildQueueName(string validationBuildQueueName) => Update(validationBuildQueueName: validationBuildQueueName);

        public RoslynInsertionToolOptions WithQueueValidationBuild(bool queueValidationBuild) => Update(queueValidationBuild: queueValidationBuild);

        public RoslynInsertionToolOptions WithInsertToolset(bool insertToolset) => Update(insertToolset: insertToolset);

        public RoslynInsertionToolOptions WithInsertedBuildRetained(bool retainInsertedBuild) => Update(retainInsertedBuild: retainInsertedBuild);

        public RoslynInsertionToolOptions WithVisualStudioRepoAzdoUsername(string visualStudioRepoAzdoUsername) => Update(visualStudioRepoAzdoUsername: visualStudioRepoAzdoUsername);

        public RoslynInsertionToolOptions WithVisualStudioRepoAzdoPassword(string visualStudioRepoAzdoPassword) => Update(visualStudioRepoAzdoPassword: visualStudioRepoAzdoPassword);

        public RoslynInsertionToolOptions WithVisualStudioRepoAzdoUri(string visualStudioRepoAzdoUri) => Update(visualStudioRepoAzdoUri: visualStudioRepoAzdoUri);

        public RoslynInsertionToolOptions WithVisualStudioRepoProjectName(string visualStudioProjectName) => Update(visualStudioProjectName: visualStudioProjectName);

        public RoslynInsertionToolOptions WithVisualStudioBranchName(string visualStudioBranchName) => Update(visualStudioBranchName: visualStudioBranchName);

        public RoslynInsertionToolOptions WithComponentBuildAzdoUsername(string componentBuildAzdoUsername) => Update(componentBuildAzdoUsername: componentBuildAzdoUsername);

        public RoslynInsertionToolOptions WithComponentBuildAzdoPassword(string componentBuildAzdoPassword) => Update(componentBuildAzdoPassword: componentBuildAzdoPassword);

        public RoslynInsertionToolOptions WithComponentBuildAzdoUri(string componentBuildAzdoUri) => Update(componentBuildAzdoUri: componentBuildAzdoUri);

        public RoslynInsertionToolOptions WithComponentBuildProjectName(string componentBuildProjectName) => Update(componentBuildProjectName: componentBuildProjectName);

        public RoslynInsertionToolOptions WithComponentBuildQueueName(string componentBuildQueueName) => Update(componentBuildQueueName: componentBuildQueueName);

        public RoslynInsertionToolOptions WithComponentBranchName(string componentBranchName) => Update(componentBranchName: componentBranchName);

        public RoslynInsertionToolOptions WithComponentGitHubRepoName(string componentGitHubRepoName) => Update(componentGitHubRepoName: componentGitHubRepoName);

        public RoslynInsertionToolOptions WithBuildConfig(string buildConfig) => Update(buildConfig: buildConfig);

        public RoslynInsertionToolOptions WithInsertionBranchName(string insertionBranchName) => Update(insertionBranchName: insertionBranchName);

        public RoslynInsertionToolOptions WithBuildDropPath(string buildDropPath) => Update(buildDropPath: buildDropPath);

        public RoslynInsertionToolOptions WithSpecificBuild(string specificBuild) => Update(specificBuild: specificBuild);

        public RoslynInsertionToolOptions WithInsertCoreXTPackages(bool insertCoreXTPackages) => Update(insertCoreXTPackages: insertCoreXTPackages);

        public RoslynInsertionToolOptions WithInsertDevDivSourceFiles(bool insertDevDivSourceFiles) => Update(insertDevDivSourceFiles: insertDevDivSourceFiles);

        public RoslynInsertionToolOptions WithInsertWillowPackages(bool insertWillowPackages) => Update(insertWillowPackages: insertWillowPackages);

        public RoslynInsertionToolOptions WithInsertionName(string insertionName) => Update(insertionName: insertionName);

        public RoslynInsertionToolOptions WithUpdateCoreXTLLibraries(bool updateCoreXTLibraries) => Update(updateCoreXTLibraries: updateCoreXTLibraries);

        public RoslynInsertionToolOptions WithUpdateAssemblyVersions(bool updateAssemblyVersions) => Update(updateAssemblyVersions: updateAssemblyVersions);

        public RoslynInsertionToolOptions WithCreateDummyPr(bool createDummyPr) => Update(createDummyPr: createDummyPr);

        public RoslynInsertionToolOptions WithUpdateExistingPr(int updateExistingPr) => Update(updateExistingPr: updateExistingPr);

        public RoslynInsertionToolOptions WithOverwritePr(bool overwritePr) => Update(overwritePr: overwritePr);

        public RoslynInsertionToolOptions WithLogFileLocation(string logFileLocation) => Update(logFileLocation: logFileLocation);

        public RoslynInsertionToolOptions WithClientId(string clientId) => Update(clientId: clientId);

        public RoslynInsertionToolOptions WithClientSecret(string clientSecret) => Update(clientSecret: clientSecret);

        public RoslynInsertionToolOptions WithTitlePrefix(string titlePrefix) => Update(titlePrefix: titlePrefix);

        public RoslynInsertionToolOptions WithCreateDraftPr(bool createDraftPr) => Update(createDraftPr: createDraftPr);

        public RoslynInsertionToolOptions WithSetAutoComplete(bool setAutoComplete) => Update(setAutoComplete: setAutoComplete);

        public RoslynInsertionToolOptions WithCherryPick(ImmutableArray<string> cherryPick) => Update(cherryPick: cherryPick);

        public RoslynInsertionToolOptions WithSkipCoreXTPackages(string skipCoreXTPackages) =>
            Update(skipCoreXTPackages: (skipCoreXTPackages ?? string.Empty).Split(',').Select(packageName => packageName.Trim()).Where(packageName => !string.IsNullOrEmpty(packageName)).ToImmutableArray());

        public string VisualStudioRepoAzdoUsername { get; }

        public string VisualStudioRepoAzdoPassword { get; }

        public string VisualStudioRepoAzdoUri { get; }

        public string VisualStudioRepoProjectName { get; }

        public string VisualStudioBranchName { get; }

        public string ComponentBuildAzdoUsername { get; }

        public string ComponentBuildAzdoPassword { get; }

        public string ComponentBuildAzdoUri { get; }

        public string ComponentBuildProjectName { get; }

        public string ComponentBuildProjectNameOrFallback
            => ComponentBuildProjectName ?? VisualStudioRepoProjectName;

        public string ComponentBuildQueueName { get; }

        public string ComponentBranchName { get; }

        public string ComponentGitHubRepoName { get; }

        public string BuildConfig { get; }

        public string InsertionBranchName { get; }

        public string BuildDropPath { get; }

        public string SpecificBuild { get; }

        public bool InsertCoreXTPackages { get; }

        public bool UpdateCoreXTLibraries { get; }

        public bool UpdateAssemblyVersions { get; }

        public bool InsertDevDivSourceFiles { get; }

        public bool InsertWillowPackages { get; }

        public string InsertionName { get; }

        public bool InsertToolset { get; }

        public bool RetainInsertedBuild { get; }

        public bool QueueValidationBuild { get; }

        public string ValidationBuildQueueName { get; }

        public bool RunDDRITsInValidation { get; }

        public bool RunRPSInValidation { get; }

        public bool CreateDummyPr { get; }

        public int UpdateExistingPr { get; }

        public bool OverwritePr { get; }

        public string LogFileLocation { get; }

        public string ClientId { get; }

        public string ClientSecret { get; }

        public string TitlePrefix { get; }

        public bool CreateDraftPr { get; }

        public bool SetAutoComplete { get; }

        public ImmutableArray<string> CherryPick { get; }

        public ImmutableArray<string> SkipCoreXTPackages { get; }

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
