using Newtonsoft.Json;

namespace DirtBag.Modules {
    public interface IModuleSettings {
		[JsonProperty]
        bool Enabled { get; set; }
		[JsonProperty]
		int EveryXRuns { get; set; }
		[JsonConverter( typeof( PostTypeConverter ) )]
		[JsonProperty]
		PostType PostTypes { get; set; }

        void SetDefaultSettings();
    }
}