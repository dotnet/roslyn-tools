// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace GithubMergeTool
{
    interface IHttpClientDecorator
    {
        Task<HttpResponseMessage> PostAsyncAsJson(string requestUri, string body);

        Task<HttpResponseMessage> DeleteAsync(string requestUri);

        Task<HttpResponseMessage> GetAsync(string requestUri);
    }
}
