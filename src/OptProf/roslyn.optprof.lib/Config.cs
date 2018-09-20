using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using roslyn.optprof.json;
using System;
using System.IO;

namespace roslyn.optprof.lib
{
    public static class Config
    {
        public static (bool success, OptProfTrainingConfiguration config) TryReadConfigFile(TextReader configReader)
        {
            try
            {
                var config = JsonSerializer.CreateDefault().Deserialize<OptProfTrainingConfiguration>(configReader);
                return (true, config);
            }
            catch (Exception)
            {
                return (false, null);
            }

        }
    }
}
