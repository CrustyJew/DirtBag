using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBagWebservice.Models {
    public class YouTubeSpamDetectorSettings : IModuleSettings {
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonProperty]
        public double ScoreMultiplier { get; set; }
        [JsonProperty]
        public DateTime LastModified { get; set; }
        [JsonProperty]
        public string ModifiedBy { get; set; }
        [JsonProperty]
        public Flair RemovalFlair { get; set; }

        /// <summary>
        /// YouTube channel age in days
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule ChannelAgeThreshold { get; set; }

        /// <summary>
        /// Video views
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule ViewCountThreshold { get; set; }
        /// <summary>
        /// Total likes and dislikes
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule VoteCountThreshold { get; set; }
        /// <summary>
        /// Bool, true will enable checking if there are more dislikes than likes
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule NegativeVoteRatio { get; set; }
        /// <summary>
        /// Reddit account age in days
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule RedditAccountAgeThreshold { get; set; }
        /// <summary>
        /// Bool, true will enable checking if the channel is likely monetized or the video claimed by a third party
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule LicensedChannel { get; set; }

        /// <summary>
        /// Integer representing percentage of
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule ChannelSubscribersThreshold { get; set; }
        /// <summary>
        /// Number of comments on video
        /// </summary>
        [JsonProperty]
        public YouTubeSpamDetectorModule CommentCountThreshold { get; set; }


        public YouTubeSpamDetectorSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            ScoreMultiplier = 1;
            ChannelAgeThreshold = new YouTubeSpamDetectorModule { Name = "ChannelAgeThreshold", Value = 14, Enabled = true, Weight = 3 };
            ViewCountThreshold = new YouTubeSpamDetectorModule { Name = "ViewCountThreshold", Value = 200, Enabled = true, Weight = 1 };
            VoteCountThreshold = new YouTubeSpamDetectorModule { Name = "VoteCountThreshold", Value = 25, Enabled = true, Weight = 1 };
            NegativeVoteRatio = new YouTubeSpamDetectorModule { Name = "NegativeVoteRatio", Enabled = true, Weight = 1 };
            RedditAccountAgeThreshold = new YouTubeSpamDetectorModule { Name = "RedditAccountAgeThreshold", Value = 30, Enabled = true, Weight = 2 };
            LicensedChannel = new YouTubeSpamDetectorModule { Name = "LicensedChannel", Enabled = true, Weight = 1 };
            ChannelSubscribersThreshold = new YouTubeSpamDetectorModule { Name = "ChannelSubscribersThreshold", Value = 25, Enabled = true, Weight = 1 };
            CommentCountThreshold = new YouTubeSpamDetectorModule { Name = "CommentCountThreshold", Value = 10, Enabled = false, Weight = 1 };
        }
    }
    public class YouTubeSpamDetectorModule {
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public int Value { get; set; }
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonProperty]
        public double Weight { get; set; }
    }
}
