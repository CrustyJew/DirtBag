using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Dirtbag.Models {
    public interface IModuleSettings {
		[JsonProperty]
        bool Enabled { get; set; }
        [JsonProperty]
        double ScoreMultiplier { get; set; }

        [JsonProperty]
        DateTime LastModified { get; set; }
        [JsonProperty]
        string ModifiedBy { get; set; }

        void SetDefaultSettings();
    }
}