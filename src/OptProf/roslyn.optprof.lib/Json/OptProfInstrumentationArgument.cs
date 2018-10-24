using Newtonsoft.Json;

namespace roslyn.optprof.json
{
    public sealed class OptProfInstrumentationArgument
    {
        [JsonProperty(PropertyName = "relativeInstallationFolder", Order = 3)]
        public string RelativeInstallationFolder { get; set; }

        [JsonProperty(PropertyName = "instrumentationExecutable", Order = 3)]
        public string InstrumentationExecutable { get; set; }
    }
}
