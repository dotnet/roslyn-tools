﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using Newtonsoft.Json;

namespace roslyn.optprof.json
{
    public sealed class AssemblyOptProfTraining
    {
        [JsonProperty(PropertyName = "assembly")]
        public string Assembly { get; set; }

        [JsonProperty(PropertyName = "instrumentationArguments")]
        public OptProfInstrumentationArgument[] InstrumentationArguments { get; set; }

        [JsonProperty(PropertyName = "tests")]
        public OptProfTrainingTest[] Tests { get; set; }
    }
}
