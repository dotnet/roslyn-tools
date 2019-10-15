// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
