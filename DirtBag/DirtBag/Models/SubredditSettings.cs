using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.Models {
    public class SubredditSettings {
        public string Subreddit { get; set; }
        [JsonIgnore]
        public bool Enabled { get; set; }
        [JsonIgnore]
        public string BotName { get; set; }
        [JsonIgnore]
        public string BotPass { internal get; set; }
        [JsonIgnore]
        public string BotAppID { internal get; set; }
        [JsonIgnore]
        public string BotAppSecret { internal get; set; }
        public double ReportScoreThreshold { get; set; }
        public double RemoveScoreThreshold { get; set; }
        public DateTime LastModified { get; set; }
        public string ModifiedBy { get; internal set; }

        /*** MODULE SETTINGS ***/
        public LicensingSmasherSettings LicensingSmasher { get; set; }
        public YouTubeSpamDetectorSettings YouTubeSpamDetector { get; set; }
        public SelfPromotionCombustorSettings SelfPromotionCombustor { get; set; }


        public static SubredditSettings GetDefaultSettings() {
            SubredditSettings toReturn = new Models.SubredditSettings();
            toReturn.RemoveScoreThreshold = -1;
            toReturn.ReportScoreThreshold = -1;
            toReturn.LicensingSmasher = new LicensingSmasherSettings();
            toReturn.LicensingSmasher.SetDefaultSettings();
            toReturn.SelfPromotionCombustor = new SelfPromotionCombustorSettings();
            toReturn.SelfPromotionCombustor.SetDefaultSettings();
            toReturn.YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
            toReturn.YouTubeSpamDetector.SetDefaultSettings();
            return toReturn;
        }
    }
}
