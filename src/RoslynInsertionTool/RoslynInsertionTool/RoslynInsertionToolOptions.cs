// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            string roslynBuildQueueName,
            string roslynBranchName,
            string roslynBuildConfig,
            string vstsUri,
            string tfsProjectName,
            string newBranchName,
            string roslynDropPath,
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
            RoslynBuildQueueName = roslynBuildQueueName;
            RoslynBranchName = roslynBranchName;
            RoslynBuildConfig = roslynBuildConfig;
            VSTSUri = vstsUri;
            TFSProjectName = tfsProjectName;
            NewBranchName = newBranchName;
            RoslynDropPath = roslynDropPath;
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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

        public RoslynInsertionToolOptions WithRoslynBuildQueueName(string roslynBuildQueueName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                roslynBuildQueueName: roslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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

        public RoslynInsertionToolOptions WithRoslynBranchName(string roslynBranchName)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: roslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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

        public RoslynInsertionToolOptions WithRoslynBuildConfig(string roslynBuildConfig)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: roslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: vstsUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: tfsProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: newBranchName,
                roslynDropPath: RoslynDropPath,
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

        public RoslynInsertionToolOptions WithRoslynDropPath(string roslynDropPath)
        {
            return new RoslynInsertionToolOptions(
                enlistmentPath: EnlistmentPath,
                username: Username,
                password: Password,
                visualStudioBranchName: VisualStudioBranchName,
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: roslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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
                roslynBuildQueueName: RoslynBuildQueueName,
                roslynBranchName: RoslynBranchName,
                roslynBuildConfig: RoslynBuildConfig,
                vstsUri: VSTSUri,
                tfsProjectName: TFSProjectName,
                newBranchName: NewBranchName,
                roslynDropPath: RoslynDropPath,
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

        public string RoslynBuildQueueName { get; }

        public string RoslynBranchName { get; }

        public string RoslynBuildConfig { get; }

        public string VSTSUri { get; }

        public string TFSProjectName { get; }

        public string NewBranchName { get; }

        public string RoslynDropPath { get; }

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
            !string.IsNullOrEmpty(RoslynBuildQueueName) &&
            !string.IsNullOrEmpty(RoslynBranchName) &&
            !string.IsNullOrEmpty(RoslynBuildConfig) &&
            !string.IsNullOrEmpty(VSTSUri) &&
            !string.IsNullOrEmpty(TFSProjectName) &&
            !string.IsNullOrEmpty(RoslynDropPath);

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

                if (string.IsNullOrEmpty(RoslynBuildQueueName))
                {
                    builder.AppendLine($"{nameof(RoslynBuildQueueName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(RoslynBranchName))
                {
                    builder.AppendLine($"{nameof(RoslynBranchName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(RoslynBuildConfig))
                {
                    builder.AppendLine($"{nameof(RoslynBuildConfig).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(VSTSUri))
                {
                    builder.AppendLine($"{nameof(VSTSUri).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(TFSProjectName))
                {
                    builder.AppendLine($"{nameof(TFSProjectName).ToLowerInvariant()} is required");
                }

                if (string.IsNullOrEmpty(RoslynDropPath))
                {
                    builder.AppendLine($"{nameof(RoslynDropPath).ToLowerInvariant()} is required");
                }

                return builder.ToString();
            }
        }
    }
}
