using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models {
    public class LicensingSmasherSettings : IModuleSettings {
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonProperty]
        public Flair RemovalFlair { get; set; }
        [JsonProperty]
        public List<string> MatchTerms { get; set; }
        [JsonProperty]
        public List<KeyValuePair<string, string>> KnownLicensers { get; set; }
        [JsonProperty]
        public double ScoreMultiplier { get; set; }
        [JsonProperty]
        public DateTime LastModified { get; set; }
        [JsonProperty]
        public string ModifiedBy { get; set; }


        public LicensingSmasherSettings() {
            KnownLicensers = new List<KeyValuePair<string, string>>();
            MatchTerms = new List<string>();
            RemovalFlair = new Flair();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            ScoreMultiplier = 1; 
            MatchTerms = new List<string> { "jukin", "licensing", "break.com", "storyful", "rumble", "newsflare", "visualdesk", "viral spiral", "viralspiral", "rightser", "to use this video in a commercial", "media enquiries" };
            //These are case sensitive for friendly name matching
            KnownLicensers = new List<KeyValuePair<string, string>> {new KeyValuePair<string, string>( "H7XeNNPkVV3JZxXm-O-MCA", "Jukin Media" ), new KeyValuePair<string, string>("Newsflare", "Newsflare" ), new KeyValuePair<string, string>("3339WgBDKIcxTfywuSmG8w", "ViralHog" ), new KeyValuePair<string, string>("Storyful", "Storyful" ), new KeyValuePair<string, string>("rumble", "Rumble" ), new KeyValuePair<string, string>("Rightster_Entertainment_Affillia", "Viral Spiral" ), new KeyValuePair<string, string>("Break", "Break" ) };
            RemovalFlair = new Flair() { Class = "red", Priority = 1, Text = "Licensed", Enabled = true };
        }
    }
}
