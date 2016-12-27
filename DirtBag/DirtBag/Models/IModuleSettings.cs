using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DirtBag.Models {
    public interface IModuleSettings {
		[JsonProperty]
        bool Enabled { get; set; }
		[JsonProperty]
		int EveryXRuns { get; set; }
        [JsonProperty]
        double ScoreMultiplier { get; set; }
		[JsonConverter( typeof( StringEnumConverter ) )]
		[JsonProperty]
		PostType PostTypes { get; set; }

        void SetDefaultSettings();
    }
}