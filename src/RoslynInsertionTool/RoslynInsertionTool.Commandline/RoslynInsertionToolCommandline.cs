// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Mono.Options;
using NLog;
using Roslyn.Insertion;
using RoslynInsertionTool;
using static Roslyn.Insertion.RoslynInsertionTool;

partial class RoslynInsertionToolCommandline
{
    public static Logger Log => LogManager.GetCurrentClassLogger();

    private static async Task MainAsync(string[] args, CancellationToken cancellationToken)
    {
        PrintSplashScreen();

        // ********************** Load Default Settings **************************
        var settings = Settings.Default;
        var options = new RoslynInsertionToolOptions()
            .WithUsername(settings.UserName)
            .WithVSTSUrl(settings.VSTSUrl)
            .WithRoslynBuildQueueName(settings.RoslynBuildQueueName)
            .WithRoslynBuildConfig(settings.RoslynBuildConfig)
            .WithEnlistmentPath(settings.EnlistmentPath)
            .WithTFSProjectName(settings.TFSProjectName)
            .WithRoslynDropPath(settings.RoslynDropPath)
            .WithNewBranchName(settings.NewBranchName)
            .WithEmailServerName(settings.EmailServerName)
            .WithMailRecipient(settings.MailRecipient)
            .WithInsertCoreXTPackages(settings.InsertCoreXTPackages)
            .WithInsertDevDivSourceFiles(settings.InsertDevDivSourceFiles)
            .WithInsertWillowPackages(settings.InsertWillowPackages)
            .WithInsertionName(settings.InsertionName)
            .WithInsertedBuildRetained(settings.RetainInsertedBuild)
            .WithQueueValidationBuild(settings.QueueValidationBuild)
            .WithValidationBuildQueueName(settings.ValidationBuildQueueName)
            .WithRunDDRITsInValidation(settings.RunDDRITsInValidation)
            .WithRunRPSInValidation(settings.RunRPSInValidation);

        // ************************ Process Arguments ****************************
        bool showHelp = false;
        cancellationToken.ThrowIfCancellationRequested();
        Log.Trace($"Processing args: {Environment.NewLine}{string.Join(Environment.NewLine, args)}");
        var parser = new OptionSet
        {
            {
                "h|?|help",
                "Show help.",
                h => showHelp = h != null
            },
            {
                "t|toolsetupdate",
                "Updates the Roslyn toolset used in the VS branch.",
                t => options = options.WithInsertToolset(true)
            },
            {
                "ep=|enlistmentpath=",
                "This is the absolute path to the Visual Studio enlistment on the machine that is running rit.exe.",
                enlistmentPath => options = options.WithEnlistmentPath(enlistmentPath)
            },
            {
                "u=|username=",
                $"Username to authenticate with VSTS *and* git. Defaults to \"{options.Username}\".",
                username => options = options.WithUsername(username)
            },
            {
                "p=|password=",
                "The password used to authenticate both VSTS *and* git. If not specified will attempt to load from Azure KeyVault.",
                password => options = options.WithPassword(password)
            },
            {
                "vsbn=|visualstudiobranchname=",
                "The Visual Studio branch we are inserting *into*.",
                visualStudioBranchName => options = options.WithVisualStudioBranchName(visualStudioBranchName)
            },
            {
                "rbn=|roslynbranchname=",
                "The Roslyn branch we are inserting *from*.",
                roslynBranchName => options = options.WithRoslynBranchName(roslynBranchName)
            },
            {
                "rbq=|roslynbuildqueue=",
                $"The name of the build queue producing signed bits you wish to insert. Defaults to \"{options.RoslynBuildQueueName}\".",
                roslynBuildQueueName => options = options.WithRoslynBuildQueueName(roslynBuildQueueName)
            },
            {
                "vstsurl=",
                $"The url to the default collection of the VSTS server. Defaults to \"{options.VSTSUri}\".",
                vstsUrl => options = options.WithVSTSUrl(vstsUrl)
            },
            {
                "tfspn=|tfsprojectname=",
                $"The project that contains the branch specified in **visualstudiobranchname**. Defaults to \"{options.TFSProjectName}\".",
                tfsProjectName => options = options.WithTFSProjectName(tfsProjectName)
            },
            {
                "nbn=|newbranchname=",
                $"The name of the branch we create when staging our insertion. Will have the current date and insertion branch appended to it. If empty a new branch and pull request are not created (for local testing purposes only). Defaults to \"{options.NewBranchName}\".",
                newBranchName => options = options.WithNewBranchName(newBranchName)
            },
            {
                "rdp=|roslyndroppath=",
                $"Location where the signed binaries are dropped. Will use this path in combination with **roslynbuildname** to find signed binaries, unless the path ends with ```Binaries\\Debug``` or ```Binaries\\Release``` (for local testing purposes only). Defaults to \"{options.RoslynDropPath}\".",
                roslynDropPath => options = options.WithRoslynDropPath(roslynDropPath)
            },
            {
                "sb=|specificbuild=",
                "Only the latest build is inserted by default, and `rit.exe` will exit if no discovered passing builds are newer than the currently inserted version. By specifying this setting `rit.exe` will skip this logic and insert the specified build.",
                specificbuild => options = options.WithSpecificBuild(specificbuild)
            },
            {
                "esn=|emailservername=",
                $"Server to use to send status emails. Defaults to \"{options.EmailServerName}\".",
                emailServerName => options = options.WithEmailServerName(emailServerName)
            },
            {
                "mr=|mailrecipient=",
                $"E-mail address to send status emails. Defaults to \"{options.MailRecipient}\".",
                mailRecipient => options = options.WithMailRecipient(mailRecipient)
            },
            {
                "ic=|insertcorextpackages=",
                $"Defaults to \"{options.InsertCoreXTPackages}\".",
                insertCoreXTPackages => options = options.WithInsertCoreXTPackages(bool.Parse(insertCoreXTPackages))
            },
            {
                "id=|insertdevdivsourcefiles=",
                $"Defaults to \"{options.InsertDevDivSourceFiles}\".",
                insertDevDivSourceFiles => options = options.WithInsertDevDivSourceFiles(bool.Parse(insertDevDivSourceFiles))
            },
            {
                "iw=|insertWillowPackages=",
                $"Defaults to \"{options.InsertWillowPackages}\".",
                insertWillowPackages => options = options.WithInsertWillowPackages(bool.Parse(insertWillowPackages))
            },
            {
                "in=|insertionName=",
                $"The \"friendly\" name of the components being inserted, e.g., Roslyn, Live Unit Testing, Project System. Defaults to \"{options.InsertionName}\".",
                insertionName=> options = options.WithInsertionName(insertionName)
            },
            {
                "ri=|retaininsertedbuild=",
                $"Whether or not the inserted build will be marked for retention. Defaults to \"{options.RetainInsertedBuild}\".",
                retainInserted => options = options.WithInsertedBuildRetained(bool.Parse(retainInserted))
            },
            {
                "qv=|queuevalidationbuild=",
                $"Creates a VS validation build of the newly created branch. A comment is added to the PR with a link to the build. RPS and DDRITs are included by default.  Defaults to \"{options.QueueValidationBuild}\".",
                queueValidationBuild => options = options.WithQueueValidationBuild(bool.Parse(queueValidationBuild))
            },
            {
                "vbq=|validationbuildqueue=",
                $"The name of the build queue to use for validation builds. Defaults to \"{options.ValidationBuildQueueName}\".",
                validationBuildQueueName => options = options.WithValidationBuildQueueName(validationBuildQueueName)
            },
            {
                "rd=|runddritsinvalidation=",
                $"Whether or not to run DDRITs as part of a validation build. Defaults to \"{options.RunDDRITsInValidation}\".",
                runDDRITsInValidation => options = options.WithRunDDRITsInValidation(bool.Parse(runDDRITsInValidation))
            },
            {
                "rr=|runrpsinvalidation=",
                $"Whether or not to run RPS tests as part of a validation build.  Defaults to \"{options.RunRPSInValidation}\".",
                runRPSInValidation => options = options.WithRunRPSInValidation(bool.Parse(runRPSInValidation))
            },
            {
                "parts=|partitions=",
                "A set of folders relative to **enlistmentpath** that should successfully build after we have inserted. List should be separated by `;`.",
                partitionsToBuild =>
                {
                    var list = options.PartitionsToBuild?.ToList() ?? new List<string>();
                    list.AddRange(partitionsToBuild.Split(';'));
                    options = options.WithPartitionsToBuild(list.ToArray());
                }
            },
            {
                "part=|partition=",
                "*Can be specified more than once.* A folder relative to **enlistmentpath** that should successfully build after we have inserted.",
                partitionToBuild =>
                {
                    var list = options.PartitionsToBuild?.ToList() ?? new List<string>();
                    list.Add(partitionToBuild);
                    options = options.WithPartitionsToBuild(list.ToArray());
                }
            },
        };

        List<string> extraArguments = null;
        try
        {
            extraArguments = parser.Parse(args);
        }
        catch (Exception e)
        {
            Log.Error("Failed to parse arguments.");
            Log.Error(e.Message);
            return;
        }

        if (extraArguments.Count > 0)
        {
            Log.Error($"Unknown arguments: {string.Join(" ", extraArguments)}");
            return;
        }

        if (showHelp)
        {
            parser.WriteOptionDescriptions(Console.Out);
            return;
        }

        if (string.IsNullOrEmpty(options.Password))
        {
            Log.Trace($"Attempting to get credentials from KeyVault.");
            try
            {
                var password = await GetSecret(settings.VsoSecretName);
                options = options.WithPassword(password);
            }
            catch (Exception e)
            {
                Log.Trace($"Failed to get credential");
                Log.Trace(e.Message);
                return;
            }
        }

        if (!options.Valid)
        {
            Log.Error(options.ValidationErrors);
            parser.WriteOptionDescriptions(Console.Out);
            return;
        }

        Log.Trace($"Processing args succeeded");

        await PerformInsertionAsync(options, Log, cancellationToken);
    }

    /// <summary>
    /// Gets the specified secret from the key vault;
    /// </summary>
    private static async Task<string> GetSecret(string secretName)
    {
        var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
        var secret = await kv.GetSecretAsync(Settings.Default.KeyVaultUrl, secretName);
        return secret.Value;
    }

    private static async Task<string> GetAccessToken(string authority, string resource, string scope)
    {
        var context = new AuthenticationContext(authority);
        AuthenticationResult authResult;
        if (string.IsNullOrEmpty(WebConfigurationManager.AppSettings["ClientId"]))
        {
            // use default domain authentication
            authResult = await context.AcquireTokenAsync(resource, Settings.Default.ApplicationId, new UserCredential());
        }
        else
        {
            // use client authentication; "ClientId" and "ClientSecret" are only available when run as a web job
            var credentials = new ClientCredential(WebConfigurationManager.AppSettings["ClientId"], WebConfigurationManager.AppSettings["ClientSecret"]);
            authResult = await context.AcquireTokenAsync(resource, credentials);
        }

        return authResult.AccessToken;
    }
}
