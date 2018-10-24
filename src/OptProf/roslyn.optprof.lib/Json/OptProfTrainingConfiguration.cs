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
