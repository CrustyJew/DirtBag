using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using DirtBag.Helpers;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;

namespace DirtBag.Modules {
    class YouTubeSpamDetector : IModule {
        public string ModuleName {
            get {
                return "YouTubeSpamDetector";
            }
        }

        public IModuleSettings Settings { get; set; }
        public Reddit RedditClient { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }

        private const int MAX_MODULE_SCORE = 10;

        public YouTubeSpamDetector() {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
        }

        public YouTubeSpamDetector( YouTubeSpamDetectorSettings settings, Reddit reddit, string sub ) : this() {
            Settings = settings;
            RedditClient = reddit;
            Subreddit = sub;
            Settings = settings;
        }
        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<Post> posts ) {
            return await Task.Run( () => {
                var toReturn = new Dictionary<string, PostAnalysisResults>();
                var youTubePosts = new Dictionary<string, List<Post>>();
                foreach ( var post in posts ) {
                    toReturn.Add( post.Id, new PostAnalysisResults( post ) );
                    var ytID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );

                    if ( !string.IsNullOrEmpty( ytID ) ) {
                        if ( !youTubePosts.ContainsKey( ytID ) ) youTubePosts.Add( ytID, new List<Post>() );
                        youTubePosts[ytID].Add( post );
                    }
                }
                var yt = new YouTubeService( new BaseClientService.Initializer { ApiKey = YouTubeAPIKey } );

                var req = yt.Videos.List( "snippet,contentDetails,statistics" );

                var settings = (YouTubeSpamDetectorSettings) Settings;
                double availWeight = 0;
                availWeight += settings.ChannelAgeThreshold.Enabled ? settings.ChannelAgeThreshold.Weight : 0;
                availWeight += settings.ViewCountThreshold.Enabled ? settings.ViewCountThreshold.Weight : 0;
                availWeight += settings.NegativeVoteRatio.Enabled ? settings.NegativeVoteRatio.Weight : 0;
                availWeight += settings.RedditAccountAgeThreshold.Enabled ? settings.RedditAccountAgeThreshold.Weight : 0;
                availWeight += settings.LicensedChannel.Enabled ? settings.LicensedChannel.Weight : 0;
                availWeight += settings.ImgurSubmissionRatio.Enabled ? settings.ImgurSubmissionRatio.Weight : 0;
                availWeight += settings.CommentCountThreshold.Enabled ? settings.CommentCountThreshold.Weight : 0;
                availWeight += settings.VoteCountThreshold.Enabled ? settings.VoteCountThreshold.Weight : 0;

                var chanAgeScore = ( settings.ChannelAgeThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
                var viewCountScore = ( settings.ViewCountThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
                var negativeVoteScore = ( settings.NegativeVoteRatio.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
                var redditAccountAgeScore = ( settings.RedditAccountAgeThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
                var licensedScore = ( settings.LicensedChannel.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
                var imgurSubmissionRatioScore = ( settings.ImgurSubmissionRatio.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
                var commentCountScore = ( settings.CommentCountThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
                var totalVotesScore = ( settings.VoteCountThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;

                var channels = new Dictionary<string, List<Post>>();
                for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                    req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                    var response = req.Execute();

                    foreach ( var vid in response.Items ) {
                        foreach ( var post in youTubePosts[vid.Id] ) {
                            var scores = toReturn[post.Id].Scores;
                            if ( !channels.ContainsKey( vid.Snippet.ChannelId ) ) {
                                channels[vid.Snippet.ChannelId] = new List<Post>();
                            }
                            channels[vid.Snippet.ChannelId].Add( post );

                            if ( settings.ViewCountThreshold.Enabled && vid.Statistics.ViewCount.Value <= (ulong) Math.Abs( settings.ViewCountThreshold.Value ) ) { //TODO Fix this math.abs nonsense with some validation
                                scores.Add( new AnalysisScore( viewCountScore, "View Count is below threshold", "Low Views", ModuleName ) );
                            }
                            if ( settings.LicensedChannel.Enabled && vid.ContentDetails.LicensedContent.Value ) {
                                scores.Add( new AnalysisScore( licensedScore, "Channel is likely monetized", "Possibly Monetized", ModuleName ) );
                            }
                            if ( settings.CommentCountThreshold.Enabled && vid.Statistics.CommentCount <= (ulong) Math.Abs( settings.CommentCountThreshold.Value ) ) { //TODO Fix this math.abs nonsense with some validation
                                scores.Add( new AnalysisScore( commentCountScore, "Number of comments is below threshold", "Low comments", ModuleName ) );
                            }
                            if ( settings.NegativeVoteRatio.Enabled && vid.Statistics.DislikeCount > vid.Statistics.LikeCount ) {
                                scores.Add( new AnalysisScore( negativeVoteScore, "More dislikes than likes on video", ">50% dislikes", ModuleName ) );
                            }
                            if ( settings.VoteCountThreshold.Enabled && vid.Statistics.DislikeCount + vid.Statistics.LikeCount <= (ulong) Math.Abs( settings.VoteCountThreshold.Value ) ) { //TODO Fix this math.abs nonsense with some validation
                                scores.Add( new AnalysisScore( totalVotesScore, "Total vote count is below threshold", "Low Total Votes", ModuleName ) );
                            }
                            if ( settings.RedditAccountAgeThreshold.Enabled && post.Author.Created.AddDays( settings.RedditAccountAgeThreshold.Value ) >= DateTime.UtcNow ) {
                                scores.Add( new AnalysisScore( redditAccountAgeScore, "Reddit Account age is below threshold", "New Reddit Acct", ModuleName ) );
                            }
                            if ( settings.ImgurSubmissionRatio.Enabled && ( (double) 100 / post.Author.Posts.Take( 100 ).Count( p => p.Domain.ToLower().Contains( "imgur" ) ) ) * 100 >= settings.ImgurSubmissionRatio.Value ) {
                                scores.Add( new AnalysisScore( imgurSubmissionRatioScore, "User has Imgur submissions above threshold for last 100 posts", "Lots of Imgur", ModuleName ) );
                            }
                        }
                    }
                    if ( settings.ChannelAgeThreshold.Enabled ) {

                        var chanReq = yt.Channels.List( "snippet" );
                        chanReq.Id = string.Join( ",", channels.Keys );
                        var chanResponse = chanReq.Execute();
                        //get the channel info
                        foreach ( var channel in chanResponse.Items ) {
                            //if the channel was created less than the settings.ChannelAgeThreshold days ago
                            DateTime channelCreationDate = channel.Snippet.PublishedAt.HasValue ? channel.Snippet.PublishedAt.Value : DateTime.UtcNow;
                            foreach ( var post in channels[channel.Id] ) {
                                if ( channelCreationDate.AddDays( settings.ChannelAgeThreshold.Value ) >= post.CreatedUTC ) {
                                
                                    //Add the score to the posts
                                    toReturn[post.Id].Scores.Add( new AnalysisScore( chanAgeScore, "Channel Age Below Threshold", "Channel Age", ModuleName ) );
                                }
                            }
                        }
                    }

                }
                return toReturn;
            } );
        }
    }

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
