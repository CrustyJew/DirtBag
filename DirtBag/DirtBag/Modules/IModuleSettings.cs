using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DirtBag.Modules {
    public interface IModuleSettings {
		[JsonProperty]
        bool Enabled { get; set; }
		[JsonProperty]
		int EveryXRuns { get; set; }
		[JsonConverter( typeof( StringEnumConverter ) )]
		[JsonProperty]
		PostType PostTypes { get; set; }

        void SetDefaultSettings();
    }
}