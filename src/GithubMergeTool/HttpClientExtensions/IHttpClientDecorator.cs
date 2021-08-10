// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

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
