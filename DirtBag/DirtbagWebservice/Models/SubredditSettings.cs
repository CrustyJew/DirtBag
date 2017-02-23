using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBagWebservice.Models {
    public class SubredditSettings {
        [JsonProperty]
        public string Subreddit { get; set; }
        [JsonProperty]
        public double ReportScoreThreshold { get; set; }
        [JsonProperty]
        public double RemoveScoreThreshold { get; set; }
        [JsonProperty]
        public DateTime LastModified { get; set; }
        [JsonProperty]
        public string ModifiedBy { get; set; }

        /*** MODULE SETTINGS ***/
        [JsonProperty]
        public LicensingSmasherSettings LicensingSmasher { get; set; }
        [JsonProperty]
        public YouTubeSpamDetectorSettings YouTubeSpamDetector { get; set; }
        [JsonProperty]
        public SelfPromotionCombustorSettings SelfPromotionCombustor { get; set; }


        public static SubredditSettings GetDefaultSettings() {
            SubredditSettings toReturn = new Models.SubredditSettings();
            toReturn.RemoveScoreThreshold = -1;
            toReturn.ReportScoreThreshold = -1;
            toReturn.LicensingSmasher = new LicensingSmasherSettings();
            toReturn.SelfPromotionCombustor = new SelfPromotionCombustorSettings();
            toReturn.YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
            return toReturn;
        }
    }
}
