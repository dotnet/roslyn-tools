// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Mono.Options;

using Roslyn.Insertion;
using static Roslyn.Insertion.RoslynInsertionTool;

partial class RoslynInsertionToolCommandline
{
    private static async Task<bool> MainAsync(string[] args, CancellationToken cancellationToken)
    {
        // ********************** Load Default Settings **************************
        var settings = Settings.Default;
        var options = new RoslynInsertionToolOptions()
            .WithVisualStudioRepoAzdoUsername(settings.VisualStudioRepoAzdoUserName)
            .WithVisualStudioRepoAzdoUri(settings.VisualStudioRepoAzdoUri)
            .WithVisualStudioRepoProjectName(settings.VisualStudioRepoProjectName)
            .WithComponentBuildQueueName(settings.BuildQueueName)
            .WithBuildConfig(settings.BuildConfig)
            .WithBuildDropPath(settings.BuildDropPath)
            .WithInsertionBranchName(settings.InsertionBranchName)
            .WithInsertCoreXTPackages(settings.InsertCoreXTPackages)
            .WithUpdateCoreXTLLibraries(settings.UpdateCoreXTLibraries)
            .WithInsertDevDivSourceFiles(settings.InsertDevDivSourceFiles)
            .WithInsertWillowPackages(settings.InsertWillowPackages)
            .WithInsertionName(settings.InsertionName)
            .WithInsertedBuildRetained(settings.RetainInsertedBuild)
            .WithQueueValidationBuild(settings.QueueValidationBuild)
            .WithValidationBuildQueueName(settings.ValidationBuildQueueName)
            .WithRunDDRITsInValidation(settings.RunDDRITsInValidation)
            .WithRunRPSInValidation(settings.RunRPSInValidation)
            .WithLogFileLocation(settings.LogFileLocation)
            .WithCreateDraftPr(settings.CreateDraftPr)
            .WithSkipCoreXTPackages(settings.SkipCoreXTPackages);

        // ************************ Process Arguments ****************************
        bool showHelp = false;
        string pullRequestUrlFile = null;
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"Processing args: {Environment.NewLine}{string.Join(Environment.NewLine, args)}");
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
                "u=|username=|visualstudiorepoazdousername=",
                $"Username to authenticate with AzDO *and* git. Defaults to \"{options.VisualStudioRepoAzdoUsername}\".",
                visualStudioRepoAzdoUsername => options = options.WithVisualStudioRepoAzdoUsername(visualStudioRepoAzdoUsername)
            },
            {
                "p=|password=|visualstudiorepoazdopassword=",
                "The password used to authenticate both AzDO *and* git. If not specified will attempt to load from Azure KeyVault.",
                visualStudioRepoAzdoPassword => options = options.WithVisualStudioRepoAzdoPassword(visualStudioRepoAzdoPassword)
            },
            {
                "vstsurl=|visualstudiorepoazdouri=",
                $"The url to the default collection of the AzDO server. Defaults to \"{options.VisualStudioRepoAzdoUri}\".",
                visualStudioRepoAzdoUri => options = options.WithVisualStudioRepoAzdoUri(visualStudioRepoAzdoUri)
            },
            {
                "tfspn=|tfsprojectname=|vspn=|visualstudiorepoprojectname=",
                $"The project that contains the branch specified in **visualstudiobranchname**. Defaults to \"{options.VisualStudioRepoProjectName}\".",
                visualStudioRepoProjectName => options = options.WithVisualStudioRepoProjectName(visualStudioRepoProjectName)
            },
            {
                "vsbn=|visualstudiobranchname=",
                "The Visual Studio branch we are inserting *into*.",
                visualStudioBranchName => options = options.WithVisualStudioBranchName(visualStudioBranchName)
            },
            {
                "cbu=|componentbuildazdousername=",
                $"Username to authenticate with the Component Build AzDO. Required if **componentbuildazdouri** is specified.",
                componentBuildAzdoUsername => options = options.WithComponentBuildAzdoUsername(componentBuildAzdoUsername)
            },
            {
                "cbp=|componentbuildazdopassword=",
                "The password used to authenticate with the Component Build AzDO. Required if **componentbuildazdouri** is specified.",
                componentBuildAzdoPassword => options = options.WithComponentBuildAzdoPassword(componentBuildAzdoPassword)
            },
            {
                "cburi=|componentbuildazdouri=",
                $"The url to the default collection of the AzDO server containing the build you wish to insert. Defaults to the **visualstudiorepoazdouri** if unspecified.",
                componentBuildAzdoUri => options = options.WithComponentBuildAzdoUri(componentBuildAzdoUri)
            },
            {
                "cbpn=|componentbuildprojectname=",
                $"The name of the build queue producing signed bits you wish to insert. Defaults to match the **visualstudiorepoprojectname** option.",
                componentBuildProjectName => options = options.WithComponentBuildProjectName(componentBuildProjectName)
            },
            {
                "bq=|buildqueue=|componentbuildqueue=",
                $"The name of the build queue producing signed bits you wish to insert. Defaults to \"{options.ComponentBuildQueueName}\".",
                componentBuildQueueName => options = options.WithComponentBuildQueueName(componentBuildQueueName)
            },
            {
                "bn=|branchname=|componentbranchname=",
                "The branch we are inserting *from*.",
                componentBranchName => options = options.WithComponentBranchName(componentBranchName)
            },
            {
                "componentgithubreponame=",
                "The github repo name that hosts the component's source code.",
                componentGitHubRepoName => options = options.WithComponentGitHubRepoName(componentGitHubRepoName)
            },
            {
                "nbn=|newbranchname=|insertionbranchname=",
                $"The name of the branch we create when staging our insertion. Will have the current date and insertion branch appended to it. If empty a new branch and pull request are not created (for local testing purposes only). Defaults to \"{options.InsertionBranchName}\".",
                insertionBranchName => options = options.WithInsertionBranchName(insertionBranchName)
            },
            {
                "dp=|droppath=",
                $"Location where the signed binaries are dropped. Will use this path in combination with **branchname** to find signed binaries, " +
                $"unless the path ends with `artifacts\\VSSetup\\Debug` or `artifacts\\VSSetup\\Release` in Arcade repositories, or " +
                $"`Binaries\\Debug` or `Binaries\\Release` in legacy repositories (for local testing purposes only). Defaults to \"{options.BuildDropPath}\".",
                dropPath => options = options.WithBuildDropPath(dropPath)
            },
            {
                "sb=|specificbuild=",
                "Only the latest build is inserted by default, and `rit.exe` will exit if no discovered passing builds are newer than the currently inserted version. By specifying this setting `rit.exe` will skip this logic and insert the specified build.",
                specificbuild => options = options.WithSpecificBuild(specificbuild)
            },
            {
                "ic=|insertcorextpackages=",
                $"Defaults to \"{options.InsertCoreXTPackages}\".",
                insertCoreXTPackages => options = options.WithInsertCoreXTPackages(bool.Parse(insertCoreXTPackages))
            },
            {
                "uc=|updatecorextlibraries=",
                $"Updates a props file used by CoreXT for generating links to library paths. Defaults to \"{options.UpdateCoreXTLibraries}\".",
                updateCoreXTLibraries => options = options.WithUpdateCoreXTLLibraries(bool.Parse(updateCoreXTLibraries))
            },
            {
                "ua=|updateassemblyversions=",
                $"Updates the binding redirects for the insertion components. Defaults to \"{options.UpdateAssemblyVersions}\".",
                updateAssemblyVersions => options = options.WithUpdateAssemblyVersions(bool.Parse(updateAssemblyVersions))
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
                "dm|createdummypr",
                $"Create a dummy insertion PR that will be updated later.",
                createDummyPr => options = options.WithCreateDummyPr(true)
            },
            {
                "upr=|updateexistingpr=",
                "Update the specified existing PR with new build information.",
                updateExistingPr => options = options.WithUpdateExistingPr(int.Parse(updateExistingPr))
            },
            {
                "opr|overwritepr",
                "Indicates that the PR specified by \"updateexistingpr\" needs to be overwritten.",
                overwritePr => options = options.WithOverwritePr(true)
            },
            {
                "ll=|loglocation=",
                "The location of the log file to be written.  Defaults to `rit.log`.",
                logFileLocation => options = options.WithLogFileLocation(logFileLocation)
            },
            {
                "ci=|clientid=",
                "The client ID to use for authentication token retreival.",
                clientId => options = options.WithClientId(clientId)
            },
            {
                "cs=|clientsecret=",
                "The client secret to use for authentication token retreival.",
                clientSecret => options = options.WithClientSecret(clientSecret)
            },
            {
                "wpr=|writepullrequest=",
                "Write the pull request URL to the specified file.",
                prf => pullRequestUrlFile = prf
            },
            {
                "tp=|titleprefix=",
                "Prepend the generated pull request's title with the specified value.",
                titlePrefix => options = options.WithTitlePrefix(titlePrefix)
            },
            {
                "dpr=|createdraftpr=",
                "Create an insertion PR that is marked as a draft.",
                createDraftPr => options = options.WithCreateDraftPr(bool.Parse(createDraftPr))
            },
            {
                "ac=|setautocomplete=",
                $"Sets the PR to Auto-Complete once all requirements are met. Defaults to \"{options.SetAutoComplete}\".",
                setAutoComplete => options = options.WithSetAutoComplete(bool.Parse(setAutoComplete))
            },
            {
                "cherrypick=",
                $"An optional comma-separated list of VS commits to cherry-pick into the insertion.",
                cherryPick => options = options.WithCherryPick(cherryPick.Split(',').Select(sha => sha.Trim()).ToImmutableArray())
            },
            {
                "skipcorextpackages=",
                $"An optional comma-separated list of CoreXT packages to be skipped when inserting/updating.",
                skipCoreXTPackages => options = options.WithSkipCoreXTPackages(skipCoreXTPackages)
            },
        };

        List<string> extraArguments = null;
        try
        {
            extraArguments = parser.Parse(args);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to parse arguments.");
            Console.WriteLine(e.Message);
            return false;
        }

        if (extraArguments.Count > 0)
        {
            Console.WriteLine($"Unknown arguments: {string.Join(" ", extraArguments)}");
            return false;
        }

        if (showHelp)
        {
            parser.WriteOptionDescriptions(Console.Out);
            return true;
        }

        if (string.IsNullOrEmpty(options.VisualStudioRepoAzdoPassword))
        {
            if (!string.IsNullOrEmpty(options.ClientId) && !string.IsNullOrEmpty(options.ClientSecret))
            {
                Console.WriteLine($"Attempting to get credentials from KeyVault.");
                try
                {
                    var visualStudioRepoAzdoPassword = await GetSecret(settings.VisualStudioRepoSecretName, options);
                    options = options.WithVisualStudioRepoAzdoPassword(visualStudioRepoAzdoPassword);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to get VS Repo Azdo credential");
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            else
            {
                Console.Error.WriteLine("No password provided and no client secret for KeyVault provided.");
                Console.Error.WriteLine("If you want to develop the tool locally, do the following:");
                Console.Error.WriteLine("1. Go to https://devdiv.visualstudio.com/_usersSettings/tokens and generate a token with the following scopes: vso.build_execute,vso.code_full,vso.release_execute,vso.packaging");
                Console.Error.WriteLine("2. Add the command line arguments `/username=myusername@microsoft.com /password=myauthtoken`");
                return false;
            }
        }

        if (!string.IsNullOrEmpty(options.ComponentBuildAzdoUri) && string.IsNullOrEmpty(options.ComponentBuildAzdoPassword))
        {
            if (!string.IsNullOrEmpty(options.ClientId) && !string.IsNullOrEmpty(options.ClientSecret))
            {
                try
                {
                    var componentBuildAzdoPassword = await GetSecret(settings.ComponentBuildSecretName, options);
                    options = options.WithComponentBuildAzdoPassword(componentBuildAzdoPassword);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to get Component Build Azdo credential. Using VS Repo Azdo credential instead.");
                    Console.WriteLine(e.Message);
                    options = options.WithComponentBuildAzdoPassword(options.VisualStudioRepoAzdoPassword);
                }
            }
            else
            {
                Console.Error.WriteLine("No password provided and no client secret for KeyVault provided.");
                Console.Error.WriteLine("If you want to develop the tool locally, do the following:");
                Console.Error.WriteLine("1. Go to https://dnceng.visualstudio.com/_usersSettings/tokens and generate a token with the following scopes: vso.build_execute,vso.code_full,vso.release_execute,vso.packaging");
                Console.Error.WriteLine("2. Add the command line arguments `/componentbuildazdousername=myusername@microsoft.com /componentbuildazdopassword=myauthtoken`");
                return false;
            }
        }

        if (!options.Valid)
        {
            Console.WriteLine(options.ValidationErrors);
            parser.WriteOptionDescriptions(Console.Out);
            return false;
        }

        Console.WriteLine($"Processing args succeeded");

        var (success, pullRequestId) = await PerformInsertionAsync(options, cancellationToken);
        if (success && pullRequestId > 0 && !string.IsNullOrEmpty(pullRequestUrlFile))
        {
            File.WriteAllText(pullRequestUrlFile, $"https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/{pullRequestId}");
        }

        return success;
    }

    /// <summary>
    /// Gets the specified secret from the key vault;
    /// </summary>
    private static async Task<string> GetSecret(string secretName, RoslynInsertionToolOptions options)
    {
        var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenFunction(options.ClientId, options.ClientSecret)));
        var secret = await kv.GetSecretAsync(Settings.Default.KeyVaultUrl, secretName);
        return secret.Value;
    }

    private static Func<string, string, string, Task<string>> GetAccessTokenFunction(string clientId, string clientSecret)
    {
        return async (authority, resource, scope) =>
        {
            var context = new AuthenticationContext(authority);
            AuthenticationResult authResult;
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                // use default domain authentication
                authResult = await context.AcquireTokenAsync(resource, Settings.Default.ApplicationId, new UserCredential());
            }
            else
            {
                // use client authentication from command line arguments
                var credentials = new ClientCredential(clientId, clientSecret);
                authResult = await context.AcquireTokenAsync(resource, credentials);
            }

            return authResult.AccessToken;
        };
    }
}
