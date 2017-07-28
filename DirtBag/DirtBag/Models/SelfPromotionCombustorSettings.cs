using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models {
    public class SelfPromotionCombustorSettings : IModuleSettings {
        public bool Enabled { get; set; }

        [JsonProperty]
        public Flair RemovalFlair { get; set; }

        public double ScoreMultiplier { get; set; }
        [JsonProperty]
        public int PercentageThreshold { get; set; }
        [JsonProperty]
        public bool IncludePostInPercentage { get; set; }
        [JsonProperty]
        public int GracePeriod { get; set; }

        public DateTime LastModified { get; set; }

        public string ModifiedBy { get; set; }

        public SelfPromotionCombustorSettings() {
            RemovalFlair = new Flair();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            ScoreMultiplier = 1;
            PercentageThreshold = 10;
            RemovalFlair = new Flair( "10%", "red", 1 );
            IncludePostInPercentage = false;
            GracePeriod = 3;
        }
    }
}
