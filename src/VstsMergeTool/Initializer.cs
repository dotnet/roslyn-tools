using System;
using NLog;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Client;
using System.Threading.Tasks;
using System.Web.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.KeyVault;

namespace VstsMergeTool
{

    public enum Authentication
    {
        PersonalToken,
        UserNameAndPassword
    }

    public class Initializer
    {
        private Logger logger;

        public VstsMergeTool MergeTool { get; }

        private TfsTeamProjectCollection ProjectCollection;

        private Settings settings = Settings.Default;

        public Initializer(Authentication authentication)
        {
            logger = LogManager.GetCurrentClassLogger();
            logger.Info($"Auto Merging tool start on {DateTime.Now:yyMMddHHmmss}");
            if (authentication == Authentication.PersonalToken)
            {
                logger.Info("Using personal token");
                VssConnection connection = new VssConnection(new Uri($"https://{settings.AccountName}.visualstudio.com"), new VssBasicCredential("", settings.Token));

                var gitClient = connection.GetClient<GitHttpClient>();

                MergeTool = new VstsMergeTool(gitClient);
            }
            else if (authentication == Authentication.UserNameAndPassword)
            {
                logger.Info("Using UserName and Password");

                // Fetch password
                string password = GetPassword(settings.VsoSecretName).Result;

                ProjectCollection = new TfsTeamProjectCollection(
                    new Uri($"www.{settings.AccountName}.visualstudio.com/{settings.TFSProjectName}"),
                    new VssBasicCredential(settings.UserName, password));
                var gitClient = ProjectCollection.GetClient<GitHttpClient>();
                MergeTool = new VstsMergeTool(gitClient);
            }
            else
            {
                logger.Info("Please select your authentication method correctly");
            }
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
