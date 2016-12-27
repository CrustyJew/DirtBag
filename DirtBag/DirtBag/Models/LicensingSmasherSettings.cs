using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class LicensingSmasherSettings : IModuleSettings {
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonConverter( typeof( StringEnumConverter ) )]
        [JsonProperty]
        public PostType PostTypes { get; set; }
        [JsonProperty]
        public int EveryXRuns { get; set; }
        [JsonProperty]
        public Flair RemovalFlair { get; set; }
        [JsonProperty]
        public string[] MatchTerms { get; set; }
        [JsonProperty]
        public Dictionary<string, string> KnownLicensers { get; set; }

        public double ScoreMultiplier { get; set; }

        public LicensingSmasherSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            PostTypes = PostType.All;
            EveryXRuns = 1;
            ScoreMultiplier = 1;
            MatchTerms = new[] { "jukin", "licensing", "break.com", "storyful", "rumble", "newsflare", "visualdesk", "viral spiral", "viralspiral", "rightser", "to use this video in a commercial", "media enquiries" };
            //These are case sensitive for friendly name matching
            KnownLicensers = new Dictionary<string, string> { { "H7XeNNPkVV3JZxXm-O-MCA", "Jukin Media" }, { "Newsflare", "Newsflare" }, { "3339WgBDKIcxTfywuSmG8w", "ViralHog" }, { "Storyful", "Storyful" }, { "rumble", "Rumble" }, { "Rightster_Entertainment_Affillia", "Viral Spiral" }, { "Break", "Break" } };
            RemovalFlair = new Flair() { Class = "red", Priority = 1, Text = "Licensed" };
        }
    }
}
