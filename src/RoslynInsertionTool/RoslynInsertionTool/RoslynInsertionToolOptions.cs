// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;

namespace Roslyn.Insertion
{
    // borrowed from https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/Optional.cs
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
            string enlistmentPath,
            string username,
            string password,
            string visualStudioBranchName,
            string buildQueueName,
            string branchName,
            string buildConfig,
            string vstsUri,
            string tfsProjectName,
            string newBranchName,
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
            int overwriteExistingPr,
            string logFileLocation,
            string clientId,
            string clientSecret,
            params string[] partitionsToBuild)
        {
            EnlistmentPath = enlistmentPath;
            Username = username;
            Password = password;
            VisualStudioBranchName = visualStudioBranchName;
            BuildQueueName = buildQueueName;
            BranchName = branchName;
            BuildConfig = buildConfig;
            VSTSUri = vstsUri;
            TFSProjectName = tfsProjectName;
            NewBranchName = newBranchName;
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
            OverwriteExistingPr = overwriteExistingPr;
            LogFileLocation = logFileLocation;
            ClientId = clientId;
            ClientSecret = clientSecret;
            PartitionsToBuild = partitionsToBuild;
        }

        public RoslynInsertionToolOptions Update(
            Optional<string> enlistmentPath = default,
            Optional<string> username = default,
            Optional<string> password = default,
            Optional<string> visualStudioBranchName = default,
            Optional<string> buildQueueName = default,
            Optional<string> branchName = default,
            Optional<string> buildConfig = default,
            Optional<string> vstsUri = default,
            Optional<string> tfsProjectName = default,
            Optional<string> newBranchName = default,
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
            Optional<int> overwriteExistingPr = default,
            Optional<string> logFileLocation = default,
            Optional<string> clientId = default,
            Optional<string> clientSecret = default,
            Optional<string[]> partitionsToBuild = default)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: enlistmentPath.ValueOrFallback(EnlistmentPath),
                username: username.ValueOrFallback(Username),
                password: password.ValueOrFallback(Password),
                visualStudioBranchName: visualStudioBranchName.ValueOrFallback(VisualStudioBranchName),
                buildQueueName: buildQueueName.ValueOrFallback(BuildQueueName),
                branchName: branchName.ValueOrFallback(BranchName),
                buildConfig: buildConfig.ValueOrFallback(BuildConfig),
                vstsUri: vstsUri.ValueOrFallback(VSTSUri),
                tfsProjectName: tfsProjectName.ValueOrFallback(TFSProjectName),
                newBranchName: newBranchName.ValueOrFallback(NewBranchName),
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
                overwriteExistingPr: overwriteExistingPr.ValueOrFallback(OverwriteExistingPr),
                logFileLocation: logFileLocation.ValueOrFallback(LogFileLocation),
                clientId: clientId.ValueOrFallback(ClientId),
                clientSecret: clientSecret.ValueOrFallback(ClientSecret),
                partitionsToBuild: partitionsToBuild.ValueOrFallback(PartitionsToBuild));
        }

        public RoslynInsertionToolOptions WithRunRPSInValidation(bool runRPSInValidation) => Update(runRPSInValidation: runRPSInValidation);

        public RoslynInsertionToolOptions WithRunDDRITsInValidation(bool runDDRITsInValidation) => Update(runDDRITsInValidation: runDDRITsInValidation);

        public RoslynInsertionToolOptions WithValidationBuildQueueName(string validationBuildQueueName) => Update(validationBuildQueueName: validationBuildQueueName);

        public RoslynInsertionToolOptions WithQueueValidationBuild(bool queueValidationBuild) => Update(queueValidationBuild: queueValidationBuild);

        public RoslynInsertionToolOptions WithEnlistmentPath(string enlistmentPath) => Update(enlistmentPath: enlistmentPath);

        public RoslynInsertionToolOptions WithInsertToolset(bool insertToolset) => Update(insertToolset: insertToolset);

        public RoslynInsertionToolOptions WithInsertedBuildRetained(bool retainInsertedBuild) => Update(retainInsertedBuild: retainInsertedBuild);

        public RoslynInsertionToolOptions WithUsername(string username) => Update(username: username);

        public RoslynInsertionToolOptions WithPassword(string password) => Update(password: password);

        public RoslynInsertionToolOptions WithVisualStudioBranchName(string visualStudioBranchName) => Update(visualStudioBranchName: visualStudioBranchName);

        public RoslynInsertionToolOptions WithBuildQueueName(string buildQueueName) => Update(buildQueueName: buildQueueName);

        public RoslynInsertionToolOptions WithbranchName(string branchName) => Update(branchName: branchName);

        public RoslynInsertionToolOptions WithBuildConfig(string buildConfig) => Update(buildConfig: buildConfig);

        public RoslynInsertionToolOptions WithVSTSUrl(string vstsUri) => Update(vstsUri: vstsUri);

        public RoslynInsertionToolOptions WithTFSProjectName(string tfsProjectName) => Update(tfsProjectName: tfsProjectName);

        public RoslynInsertionToolOptions WithNewBranchName(string newBranchName) => Update(newBranchName: newBranchName);

        public RoslynInsertionToolOptions WithBuildDropPath(string buildDropPath) => Update(buildDropPath: buildDropPath);

        public RoslynInsertionToolOptions WithSpecificBuild(string specificBuild) => Update(specificBuild: specificBuild);

        public RoslynInsertionToolOptions WithPartitionsToBuild(params string[] partitionsToBuild) => Update(partitionsToBuild: partitionsToBuild);

        public RoslynInsertionToolOptions WithInsertCoreXTPackages(bool insertCoreXTPackages) => Update(insertCoreXTPackages: insertCoreXTPackages);

        public RoslynInsertionToolOptions WithInsertDevDivSourceFiles(bool insertDevDivSourceFiles) => Update(insertDevDivSourceFiles: insertDevDivSourceFiles);

        public RoslynInsertionToolOptions WithInsertWillowPackages(bool insertWillowPackages) => Update(insertWillowPackages: insertWillowPackages);

        public RoslynInsertionToolOptions WithInsertionName(string insertionName) => Update(insertionName: insertionName);

        public RoslynInsertionToolOptions WithUpdateCoreXTLLibraries(bool updateCoreXTLibraries) => Update(updateCoreXTLibraries: updateCoreXTLibraries);

        public RoslynInsertionToolOptions WithUpdateAssemblyVersions(bool updateAssemblyVersions) => Update(updateAssemblyVersions: updateAssemblyVersions);

        public RoslynInsertionToolOptions WithCreateDummyPr(bool createDummyPr) => Update(createDummyPr: createDummyPr);

        public RoslynInsertionToolOptions WithOverwriteExistingPr(int overwriteExistingPr) => Update(overwriteExistingPr: overwriteExistingPr);

        public RoslynInsertionToolOptions WithLogFileLocation(string logFileLocation) => Update(logFileLocation: logFileLocation);

        public RoslynInsertionToolOptions WithClientId(string clientId) => Update(clientId: clientId);

        public RoslynInsertionToolOptions WithClientSecret(string clientSecret) => Update(clientSecret: clientSecret);

        public string EnlistmentPath { get; }

        public string Username { get; }

        public string Password { get; }

        public string VisualStudioBranchName { get; }

        public string BuildQueueName { get; }

        public string BranchName { get; }

        public string BuildConfig { get; }

        public string VSTSUri { get; }

        public string TFSProjectName { get; }

        public string NewBranchName { get; }

        public string BuildDropPath { get; }

        public string SpecificBuild { get; }

        public string[] PartitionsToBuild { get; }

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

        public int OverwriteExistingPr { get; }

        public string LogFileLocation { get; }

        public string ClientId { get; }

        public string ClientSecret { get; }

        public bool Valid
        {
            get
            {
                if (CreateDummyPr)
                {
                    // only InsertionName and VisualStudioBranchName are required for creating a dummy pr
                    return
                        OverwriteExistingPr == 0 &&
                        !string.IsNullOrEmpty(InsertionName) &&
                        !string.IsNullOrEmpty(VisualStudioBranchName);
                }
                else if (OverwriteExistingPr != 0)
                {
                    // only the existing pr ID, InsertionName, BranchName, and BuildQueueName are required for overwriting an existing pr
                    return
                        !CreateDummyPr &&
                        !string.IsNullOrEmpty(InsertionName) &&
                        !string.IsNullOrEmpty(BranchName) &&
                        !string.IsNullOrEmpty(VisualStudioBranchName) &&
                        !string.IsNullOrEmpty(BuildQueueName);
                }
                else
                {
                    return
                        !string.IsNullOrEmpty(EnlistmentPath) &&
                        !string.IsNullOrEmpty(Username) &&
                        !string.IsNullOrEmpty(Password) &&
                        !string.IsNullOrEmpty(VisualStudioBranchName) &&
                        !string.IsNullOrEmpty(BuildQueueName) &&
                        !string.IsNullOrEmpty(BranchName) &&
                        !string.IsNullOrEmpty(BuildConfig) &&
                        !string.IsNullOrEmpty(VSTSUri) &&
                        !string.IsNullOrEmpty(TFSProjectName) &&
                        !string.IsNullOrEmpty(BuildDropPath);
                }
            }
        }

        public string ValidationErrors
        {
            get
            {
                var builder = new StringBuilder();

                if (CreateDummyPr)
                {
                    // only InsertionName and VisualStudioBranchName are required for creating a dummy pr
                    if (OverwriteExistingPr != 0)
                    {
                        builder.AppendLine($"{nameof(CreateDummyPr).ToLowerInvariant()} and {nameof(OverwriteExistingPr).ToLowerInvariant()} are mutually exclusive and cannot be specified together");
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
                else if (OverwriteExistingPr != 0)
                {
                    // only the existing pr ID, InsertionName, BranchName, and BuildQueueName are required for overwriting an existing pr
                    if (CreateDummyPr)
                    {
                        builder.AppendLine($"{nameof(CreateDummyPr).ToLowerInvariant()} and {nameof(OverwriteExistingPr).ToLowerInvariant()} are mutually exclusive and cannot be specified together");
                    }

                    if (string.IsNullOrEmpty(InsertionName))
                    {
                        builder.AppendLine($"{nameof(InsertionName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(BranchName))
                    {
                        builder.AppendLine($"{nameof(BranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioBranchName))
                    {
                        builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(BuildQueueName))
                    {
                        builder.AppendLine($"{nameof(BuildQueueName).ToLowerInvariant()} is required");
                    }
                }
                else
                {
                    // perform a regular insertion
                    if (string.IsNullOrEmpty(EnlistmentPath))
                    {
                        builder.AppendLine($"{nameof(EnlistmentPath).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(Username))
                    {
                        builder.AppendLine($"{nameof(Username).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(Password))
                    {
                        builder.AppendLine($"{nameof(Password).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VisualStudioBranchName))
                    {
                        builder.AppendLine($"{nameof(VisualStudioBranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(BuildQueueName))
                    {
                        builder.AppendLine($"{nameof(BuildQueueName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(BranchName))
                    {
                        builder.AppendLine($"{nameof(BranchName).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(BuildConfig))
                    {
                        builder.AppendLine($"{nameof(BuildConfig).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(VSTSUri))
                    {
                        builder.AppendLine($"{nameof(VSTSUri).ToLowerInvariant()} is required");
                    }

                    if (string.IsNullOrEmpty(TFSProjectName))
                    {
                        builder.AppendLine($"{nameof(TFSProjectName).ToLowerInvariant()} is required");
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
