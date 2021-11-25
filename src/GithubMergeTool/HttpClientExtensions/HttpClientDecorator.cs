// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Net.Http;
using System.Threading.Tasks;

namespace GithubMergeTool
{
    internal class HttpClientDecorator : IHttpClientDecorator
    {
        public HttpClient HttpClient { get; private set; }

        public HttpClientDecorator(HttpClient httpClient)
        {
            this.HttpClient = httpClient;
        }

        public Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            return HttpClient.DeleteAsync(requestUri);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return HttpClient.GetAsync(requestUri);
        }

        public Task<HttpResponseMessage> PostAsyncAsJson(string requestUri, string body)
        {
            return HttpClient.PostAsyncAsJson(requestUri, body);
        }
    }
}
