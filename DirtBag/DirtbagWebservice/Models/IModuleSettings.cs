using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DirtBagWebservice.Models {
    public interface IModuleSettings {
		[JsonProperty]
        bool Enabled { get; set; }
        [JsonProperty]
        double ScoreMultiplier { get; set; }

        void SetDefaultSettings();
    }
}