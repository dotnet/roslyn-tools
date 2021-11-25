// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using Newtonsoft.Json;

namespace roslyn.optprof.json
{
    public sealed class OptProfTrainingConfiguration
    {
        [JsonProperty(PropertyName = "products")]
        public ProductOptProfTraining[] Products { get; set; }
        [JsonProperty(PropertyName = "assemblies")]
        public AssemblyOptProfTraining[] Assemblies { get; set; }
    }
}
