// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.FileContainer.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.RoslynTools.Utilities;

internal sealed class AzDOConnection : IDisposable
{
    private bool _disposed = false;

    public string BuildProjectName { get; }
    private VssConnection Connection { get; }
    public GitHttpClient GitClient { get; }
    public BuildHttpClient BuildClient { get; }
    public HttpClient NuGetClient { get; }
    public FileContainerHttpClient ContainerClient { get; }
    public ProjectHttpClient ProjectClient { get; }

    public AzDOConnection(VssConnection vssConnection, string projectName)
    {
        Connection = vssConnection;
        BuildProjectName = projectName;

        NuGetClient = new HttpClient();

        GitClient = Connection.GetClient<GitHttpClient>();
        BuildClient = Connection.GetClient<BuildHttpClient>();

        ContainerClient = Connection.GetClient<FileContainerHttpClient>();

        ProjectClient = Connection.GetClient<ProjectHttpClient>();
    }

    public async Task<List<Build>?> TryGetBuildsAsync(string pipelineName, string? buildNumber = null, ILogger? logger = null, int? maxFetchingVsBuildNumber = null, BuildResult? resultsFilter = null, BuildQueryOrder? buildQueryOrder = null)
    {
        try
        {
            var buildDefinition = (await BuildClient.GetDefinitionsAsync(BuildProjectName, name: pipelineName)).Single();
            var builds = await BuildClient.GetBuildsAsync(
                buildDefinition.Project.Id,
                definitions: new[] { buildDefinition.Id },
                buildNumber: buildNumber,
                resultFilter: resultsFilter,
                queryOrder: buildQueryOrder,
                top: maxFetchingVsBuildNumber);
            return builds;
        }
        catch (VssUnauthorizedException ex)
        {
            logger?.LogError($"Authorization exception while retrieving builds: {ex}");
            return null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            Connection.Dispose();
            NuGetClient.Dispose();
            GitClient.Dispose();
            BuildClient.Dispose();
            ContainerClient.Dispose();
            ProjectClient.Dispose();
        }
    }
}
