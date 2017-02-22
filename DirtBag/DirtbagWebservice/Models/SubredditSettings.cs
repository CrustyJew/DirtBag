using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBagWebservice.Models {
    public class SubredditSettings {
        [JsonIgnore]
        public string Subreddit { get; set; }
        [JsonProperty]
        public double Version { get; set; }
        [JsonProperty]
        public double ReportScoreThreshold { get; set; }
        [JsonProperty]
        public double RemoveScoreThreshold { get; set; }
        [JsonIgnore]
        public DateTime LastModified { get; set; }
        /*** MODULE SETTINGS ***/
        [JsonProperty]
        public LicensingSmasherSettings LicensingSmasher { get; set; }
        [JsonProperty]
        public YouTubeSpamDetectorSettings YouTubeSpamDetector { get; set; }
        [JsonProperty]
        public SelfPromotionCombustorSettings SelfPromotionCombustor { get; set; }
    }
}
