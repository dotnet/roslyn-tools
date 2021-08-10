// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using roslyn.optprof.json;
using System;
using System.IO;

namespace roslyn.optprof.lib
{
    public static class Config
    {
        public static OptProfTrainingConfiguration ReadConfigFile(string configJson)
            => JsonSerializer.CreateDefault().Deserialize<OptProfTrainingConfiguration>(new StringReader(configJson));
    }
}
