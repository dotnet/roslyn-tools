// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
