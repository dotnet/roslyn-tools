using System;
using System.Net.Http;
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

    public class Options
    {
        public string DestBranch { get; }

        public string SourceBranch { get; }

        public string Username { get; }

        public string Token { get; }

        public string RepoId { get; }

        public string Project { get; }

        public string AccountName { get; }

        public string Reviewer { get; }

        public string Secret { get; }

        private Settings setting = Settings.Default;

        public Options()
        {
            SourceBranch = setting.SourceBranch;
            DestBranch = setting.DestBranch;
            Username = setting.UserName;
            Token = setting.Token;
            Project = setting.TFSProjectName;
            AccountName = setting.AccountName;
            Reviewer = setting.Reviewer;
            RepoId = setting.RepositoryID;
            Secret = setting.VsoSecretName;
        }
    }

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

        public Initializer(Authentication authentication)
        {
            Options options = new Options();
            logger = LogManager.GetCurrentClassLogger();
            logger.Info($"Auto Merging tool start on {DateTime.Now:yyMMddHHmmss}");
            if (authentication == Authentication.PersonalToken)
            {
                logger.Info("Using personal token");
                VssConnection connection = new VssConnection(new Uri($"https://{options.AccountName}.visualstudio.com"), new VssBasicCredential("", options.Token));

                var gitClient = connection.GetClient<GitHttpClient>();

                MergeTool = new VstsMergeTool(options, gitClient);
            }
            else if (authentication == Authentication.UserNameAndPassword)
            {
                logger.Info("Using UserName and Password");

                // Fetch password
                string password = GetPassword(options.Secret).Result;

                ProjectCollection = new TfsTeamProjectCollection(
                    new Uri($"www.{options.AccountName}.visualstudio.com/{options.Project}"),
                    new VssBasicCredential(options.Username, password));
                var gitClient = ProjectCollection.GetClient<GitHttpClient>();
                MergeTool = new VstsMergeTool(options, gitClient);
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
