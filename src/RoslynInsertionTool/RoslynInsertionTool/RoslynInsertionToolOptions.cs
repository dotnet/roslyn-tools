// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;

namespace Roslyn.Insertion
{
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
            string specificbuild,
            string emailServerName,
            string mailRecipient,
            bool insertCoreXTPackages,
            bool updateCoreXTLibraries,
            bool insertDevDivSourceFiles,
            bool insertWillowPackages,
            bool insertLibraryPackages,
            string insertionName,
            bool insertToolset,
            bool retainInsertedBuild,
            bool queueValidationBuild,
            string validationBuildQueueName,
            bool runDDRITsInValidation,
            bool runRPSInValidation,
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
            SpecificBuild = specificbuild;
            EmailServerName = emailServerName;
            MailRecipient = mailRecipient;
            InsertCoreXTPackages = insertCoreXTPackages;
            UpdateCoreXTLibraries = updateCoreXTLibraries;
            InsertDevDivSourceFiles = insertDevDivSourceFiles;
            InsertWillowPackages = insertWillowPackages;
            InsertLibraryPackages = insertLibraryPackages;
            InsertionName = insertionName;
            InsertToolset = insertToolset;
            RetainInsertedBuild = retainInsertedBuild;
            QueueValidationBuild = queueValidationBuild;
            ValidationBuildQueueName = validationBuildQueueName;
            RunDDRITsInValidation = runDDRITsInValidation;
            RunRPSInValidation = runRPSInValidation;
            PartitionsToBuild = partitionsToBuild;
        }

        public RoslynInsertionToolOptions WithRunRPSInValidation(bool runRPSInValidation)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: runRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithRunDDRITsInValidation(bool runDDRITsInValidation)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: runDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithValidationBuildQueueName(string validationBuildQueueName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: validationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithQueueValidationBuild(bool queueValidationBuild)
        {
             return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: queueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithEnlistmentPath(string enlistmentPath)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: enlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithInsertToolset(bool insertToolset) =>
            new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: true,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);

        public RoslynInsertionToolOptions WithInsertedBuildRetained(bool retainInsertedBuild) =>
            new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: retainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);

        public RoslynInsertionToolOptions WithUsername(string username)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithPassword(string password)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithVisualStudioBranchName(string visualStudioBranchName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: visualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithBuildQueueName(string buildQueueName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: buildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithbranchName(string branchName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: branchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithBuildConfig(string buildConfig)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: buildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithVSTSUrl(string vstsUri)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: vstsUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithTFSProjectName(string tfsProjectName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: tfsProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithNewBranchName(string newBranchName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: newBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithBuildDropPath(string buildDropPath)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: buildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithSpecificBuild(string specificbuild)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: specificbuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithEmailServerName(string emailServerName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: emailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithMailRecipient(string mailRecipient)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: mailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithPartitionsToBuild(params string[] partitionsToBuild)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: partitionsToBuild);
        }

        public RoslynInsertionToolOptions WithInsertCoreXTPackages(bool insertCoreXTPackages)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: insertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithInsertDevDivSourceFiles(bool insertDevDivSourceFiles)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: insertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithInsertWillowPackages(bool insertWillowPackages)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithInsertLibraryPackages(bool insertLibraryPackages)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: insertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithInsertionName(string insertionName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: UpdateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: insertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithUpdateCoreXTLLibraries(bool updateCoreXTLibraries)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                buildQueueName: BuildQueueName,
                branchName: BranchName,
                buildConfig: BuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                buildDropPath: BuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                updateCoreXTLibraries: updateCoreXTLibraries,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertLibraryPackages: InsertLibraryPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

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

        public string EmailServerName { get; }

        public string MailRecipient { get; }

        public bool InsertCoreXTPackages { get; }

        public bool UpdateCoreXTLibraries { get; }

        public bool InsertDevDivSourceFiles { get; }

        public bool InsertWillowPackages { get; }

        public bool InsertLibraryPackages { get; }

        public string InsertionName { get; }

        public bool InsertToolset { get; }

        public bool RetainInsertedBuild { get; }

        public bool QueueValidationBuild { get; }

        public string ValidationBuildQueueName { get; }

        public bool RunDDRITsInValidation { get; }

        public bool RunRPSInValidation { get; }

        public bool Valid =>
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

        public string ValidationErrors
        {
            get
            {
                var builder = new StringBuilder();
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

                return builder.ToString();
            }
        }
    }
}
