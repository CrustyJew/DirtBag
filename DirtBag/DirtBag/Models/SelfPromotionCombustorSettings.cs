using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class SelfPromotionCombustorSettings : IModuleSettings {
        public bool Enabled { get; set; }

        public int EveryXRuns { get; set; }

        public PostType PostTypes { get; set; }

        [JsonProperty]
        public Flair RemovalFlair { get; set; }

        public double ScoreMultiplier { get; set; }
        [JsonProperty]
        public int PercentageThreshold { get; set; }
        [JsonProperty]
        public bool IncludePostInPercentage { get; set; }
        [JsonProperty]
        public int GracePeriod { get; set; }

        public SelfPromotionCombustorSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            EveryXRuns = 1;
            PostTypes = PostType.New;
            ScoreMultiplier = 1;
            PercentageThreshold = 10;
            RemovalFlair = new Flair( "10%", "red", 1 );
            IncludePostInPercentage = false;
            GracePeriod = 3;
        }
    }
}
