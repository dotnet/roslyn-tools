// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Net.Http;
using System.Net;
using Azure.Security.KeyVault.Secrets;
using Microsoft.VisualStudio.Services.FileContainer.Client;
using Microsoft.TeamFoundation.Core.WebApi;

namespace VSBranchInfo
{
    internal sealed class AzDOConnection : IDisposable
    {
        private bool _disposed = false;

        public string BuildProjectName { get; }
        public string BuildDefinitionName { get; }
        private VssConnection Connection { get; }
        public GitHttpClient GitClient { get; }
        public BuildHttpClient BuildClient { get; }
        public FileContainerHttpClient ContainerClient { get; }
        public ProjectHttpClient ProjectClient { get; }

        public AzDOConnection(string azdoUrl, string projectName, string buildDefinitionName, SecretClient client, string secretName)
        {
            BuildProjectName = projectName;
            BuildDefinitionName = buildDefinitionName;

            var azureDevOpsSecret = client.GetSecret(secretName);
            var credential = new NetworkCredential("vslsnap", azureDevOpsSecret.Value.Value);

            Connection = new VssConnection(new Uri(azdoUrl), new WindowsCredential(credential));

            GitClient = Connection.GetClient<GitHttpClient>();
            BuildClient = Connection.GetClient<BuildHttpClient>();

            ContainerClient = Connection.GetClient<FileContainerHttpClient>();

            ProjectClient = Connection.GetClient<ProjectHttpClient>();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                Connection.Dispose();
                GitClient.Dispose();
                BuildClient.Dispose();
                ContainerClient.Dispose();
            }
        }
    }
}
