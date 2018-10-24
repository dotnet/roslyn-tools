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
