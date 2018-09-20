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
