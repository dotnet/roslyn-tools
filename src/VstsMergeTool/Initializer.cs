// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using NLog;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Client;
using System.Threading.Tasks;
using System.Web.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.KeyVault;

namespace VstsMergeTool
{
    public class Initializer
    {
        private Logger logger;

        public VstsMergeTool MergeTool { get; }

        private TfsTeamProjectCollection ProjectCollection;

        private Settings settings = Settings.Default;

        public Initializer(string sourceBranch, string destBranch)
        {
            logger = LogManager.GetCurrentClassLogger();
            logger.Info($"Auto Merging tool start on {DateTime.Now:MM-dd-yyyy-HH-mm-ss}");
            logger.Info($"Source branch: {sourceBranch}, Target Branch: {destBranch}");

            string password = GetPassword(settings.VsoSecretName).Result;
            ProjectCollection = new TfsTeamProjectCollection(
                new Uri($"www.{settings.AccountName}.visualstudio.com/{settings.TFSProjectName}"),
                new VssBasicCredential(settings.UserName, password));

            var gitClient = ProjectCollection.GetClient<GitHttpClient>();
            MergeTool = new VstsMergeTool(gitClient, sourceBranch, destBranch);
        }

        private static async Task<string> GetPassword(string secretName)
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
}
