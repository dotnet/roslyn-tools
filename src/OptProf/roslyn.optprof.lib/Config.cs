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
