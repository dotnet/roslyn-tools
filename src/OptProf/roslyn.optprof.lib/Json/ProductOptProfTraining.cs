using Newtonsoft.Json;

namespace roslyn.optprof.json
{
    public sealed class ProductOptProfTraining
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "tests")]
        public OptProfTrainingTest[] Tests { get; set; }
    }
}
