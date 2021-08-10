// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GithubMergeTool
{
    internal class NoOpHttpClientDecorator : IHttpClientDecorator
    {
        public NoOpHttpClientDecorator(HttpClient httpClient)
        {
            this.HttpClient = httpClient;
        }

        public HttpClient HttpClient { get; private set; }

        public Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            Debug.WriteLine($"Delete {requestUri}");
            return Task.FromResult(new HttpResponseMessage());
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return HttpClient.GetAsync(requestUri);
        }

        public Task<HttpResponseMessage> PostAsyncAsJson(string requestUri, string body)
        {
            Debug.WriteLine($"Post {requestUri} with contents {body}");
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent("{}")
            });
        }
    }
}
