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
            string githubBuildQueueName,
            string githubBranchName,
            string githubBuildConfig,
            string vstsUri,
            string tfsProjectName,
            string newBranchName,
            string githubBuildDropPath,
            string specificbuild,
            string emailServerName,
            string mailRecipient,
            bool insertCoreXTPackages,
            bool insertDevDivSourceFiles,
            bool insertWillowPackages,
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
            GithubBuildQueueName = githubBuildQueueName;
            GithubBranchName = githubBranchName;
            GithubBuildConfig = githubBuildConfig;
            VSTSUri = vstsUri;
            TFSProjectName = tfsProjectName;
            NewBranchName = newBranchName;
            GithubBuildDropPath = githubBuildDropPath;
            SpecificBuild = specificbuild;
            EmailServerName = emailServerName;
            MailRecipient = mailRecipient;
            InsertCoreXTPackages = insertCoreXTPackages;
            InsertDevDivSourceFiles = insertDevDivSourceFiles;
            InsertWillowPackages = insertWillowPackages;
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithGithubBuildQueueName(string githubBuildQueueName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                githubBuildQueueName: githubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithGitHubBranchName(string githubBranchName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: githubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithGithubBuildConfig(string githubBuildConfig)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: githubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: vstsUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: tfsProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: newBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertionName: InsertionName,
                insertToolset: InsertToolset,
                retainInsertedBuild: RetainInsertedBuild,
                queueValidationBuild: QueueValidationBuild,
                validationBuildQueueName: ValidationBuildQueueName,
                runDDRITsInValidation: RunDDRITsInValidation,
                runRPSInValidation: RunRPSInValidation,
                partitionsToBuild: PartitionsToBuild);
        }

        public RoslynInsertionToolOptions WithGithubBuildDropPath(string githubBuildDropPath)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: githubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: specificbuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: emailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: mailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: insertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: insertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: insertWillowPackages,
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
                githubBuildQueueName: GithubBuildQueueName,
                githubBranchName: GithubBranchName,
                githubBuildConfig: GithubBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                githubBuildDropPath: GithubBuildDropPath,
                specificbuild: SpecificBuild,
                emailServerName: EmailServerName,
                mailRecipient: MailRecipient,
                insertCoreXTPackages: InsertCoreXTPackages,
                insertDevDivSourceFiles: InsertDevDivSourceFiles,
                insertWillowPackages: InsertWillowPackages,
                insertionName: insertionName,
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

        public string GithubBuildQueueName { get; }

        public string GithubBranchName { get; }

        public string GithubBuildConfig { get; }

        public string VSTSUri { get; }

        public string TFSProjectName { get; }

        public string NewBranchName { get; }

        public string GithubBuildDropPath { get; }

        public string SpecificBuild { get; }

        public string[] PartitionsToBuild { get; }

        public string EmailServerName { get; }

        public string MailRecipient { get; }

        public bool InsertCoreXTPackages { get; }

        public bool InsertDevDivSourceFiles { get; }

        public bool InsertWillowPackages { get; }

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
            !string.IsNullOrEmpty(GithubBuildQueueName) &&
            !string.IsNullOrEmpty(GithubBranchName) &&
            !string.IsNullOrEmpty(GithubBuildConfig) &&
            !string.IsNullOrEmpty(VSTSUri) &&
            !string.IsNullOrEmpty(TFSProjectName) &&
            !string.IsNullOrEmpty(GithubBuildDropPath);

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

                if (string.IsNullOrEmpty(GithubBuildQueueName))
                {
                    builder.AppendLine($"{nameof(GithubBuildQueueName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(GithubBranchName))
                {
                    builder.AppendLine($"{nameof(GithubBranchName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(GithubBuildConfig))
                {
                    builder.AppendLine($"{nameof(GithubBuildConfig).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(VSTSUri))
                {
                    builder.AppendLine($"{nameof(VSTSUri).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(TFSProjectName))
                {
                    builder.AppendLine($"{nameof(TFSProjectName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(GithubBuildDropPath))
                {
                    builder.AppendLine($"{nameof(GithubBuildDropPath).ToLowerInvariant()} is required");
                }

                return builder.ToString();
            }
        }
    }
}
