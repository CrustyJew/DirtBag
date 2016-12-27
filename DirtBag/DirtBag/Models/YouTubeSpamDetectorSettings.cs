using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Models {
    public class YouTubeSpamDetectorSettings : IModuleSettings {
        public bool Enabled { get; set; }
        public PostType PostTypes { get; set; }
        public int EveryXRuns { get; set; }
        public double ScoreMultiplier { get; set; }
        /// <summary>
        /// YouTube channel age in days
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorIntCategory ChannelAgeThreshold { get; set; }

        /// <summary>
        /// Video views
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorIntCategory ViewCountThreshold { get; set; }
        /// <summary>
        /// Total likes and dislikes
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorIntCategory VoteCountThreshold { get; set; }
        /// <summary>
        /// Bool, true will enable checking if there are more dislikes than likes
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorBoolCategory NegativeVoteRatio { get; set; }
        /// <summary>
        /// Reddit account age in days
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorIntCategory RedditAccountAgeThreshold { get; set; }
        /// <summary>
        /// Bool, true will enable checking if the channel is likely monetized or the video claimed by a third party
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorBoolCategory LicensedChannel { get; set; }

        /// <summary>
        /// Integer representing percentage of
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorIntCategory ImgurSubmissionRatio { get; set; }
        /// <summary>
        /// Number of comments on video
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorIntCategory CommentCountThreshold { get; set; }


        public YouTubeSpamDetectorSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            PostTypes = PostType.New;
            EveryXRuns = 1;
            ScoreMultiplier = 1;
            ChannelAgeThreshold = new YouTubeSpamDetectorIntCategory { Value = 14, Enabled = true, Weight = 3 };
            ViewCountThreshold = new YouTubeSpamDetectorIntCategory { Value = 200, Enabled = true, Weight = 1 };
            VoteCountThreshold = new YouTubeSpamDetectorIntCategory { Value = 25, Enabled = true, Weight = 1 };
            NegativeVoteRatio = new YouTubeSpamDetectorBoolCategory { Enabled = true, Weight = 1 };
            RedditAccountAgeThreshold = new YouTubeSpamDetectorIntCategory { Value = 30, Enabled = true, Weight = 2 };
            LicensedChannel = new YouTubeSpamDetectorBoolCategory { Enabled = true, Weight = 1 };
            ImgurSubmissionRatio = new YouTubeSpamDetectorIntCategory { Value = 25, Enabled = false, Weight = 1 };
            CommentCountThreshold = new YouTubeSpamDetectorIntCategory { Value = 10, Enabled = true, Weight = 1 };
        }
    }
    public class YouTubeSpamDetectorIntCategory {
        [JsonProperty]
        public int Value { get; set; }
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonProperty]
        public double Weight { get; set; }
    }
    public class YouTubeSpamDetectorBoolCategory {
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonProperty]
        public double Weight { get; set; }
    }
}
