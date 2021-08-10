// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using Newtonsoft.Json;

namespace roslyn.optprof.json
{

    public sealed class OptProfTrainingTest
    {
        [JsonProperty(PropertyName = "container", Order = 3)]
        public string Container { get; set; }

        [JsonProperty(PropertyName = "testCases", Order = 3)]
        public string[] TestCases { get; set; }
    }
}
