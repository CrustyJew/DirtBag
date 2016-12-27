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
using System.Net;
using DirtBag.Models;

namespace DirtBag.Modules {
    class YouTubeSpamDetector : IModule {
        public string ModuleName {
            get {
                return "YouTubeSpamDetector";
            }
        }
        public Modules ModuleEnum { get { return Modules.YouTubeSpamDetector; } }
        public bool MultiScan { get { return false; } }

        public IModuleSettings Settings { get; set; }
        public Reddit RedditClient { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }

        private const int MAX_MODULE_SCORE = 10;

        private Dictionary<string, int> processedCache;

        public YouTubeSpamDetector() {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
            processedCache = new Dictionary<string, int>();
        }

        public YouTubeSpamDetector( YouTubeSpamDetectorSettings settings, Reddit reddit, string sub ) : this() {
            Settings = settings;
            RedditClient = reddit;
            Subreddit = sub;
            Settings = settings;
        }
        public async Task<Dictionary<string, AnalysisDetails>> Analyze( List<Post> posts ) {

            var toReturn = new Dictionary<string, AnalysisDetails>();
            var youTubePosts = new Dictionary<string, List<Post>>();
            foreach ( var post in posts ) {
                var ytID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );
                toReturn.Add( post.Id, new AnalysisDetails( post, ModuleEnum ) );

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

           
            for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                var channels = new Dictionary<string, List<Post>>();
                req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                var response = await req.ExecuteAsync();

                foreach ( var vid in response.Items ) {
                    foreach ( var post in youTubePosts[vid.Id] ) {
                        var scores = toReturn[post.Id].Scores;
                        if ( !channels.ContainsKey( vid.Snippet.ChannelId ) ) {
                            channels[vid.Snippet.ChannelId] = new List<Post>();
                        }
                        channels[vid.Snippet.ChannelId].Add( post );

                        if ( settings.ViewCountThreshold.Enabled && vid.Statistics.ViewCount.Value <= (ulong) Math.Abs( settings.ViewCountThreshold.Value ) ) { //TODO Fix this math.abs nonsense with some validation
                            scores.Add( new AnalysisScore( viewCountScore, "View Count is below threshold", "Low Views", ModuleEnum ) );
                        }
                        if ( settings.LicensedChannel.Enabled && vid.ContentDetails.LicensedContent.Value ) {
                            scores.Add( new AnalysisScore( licensedScore, "Channel is likely monetized", "Possibly Monetized", ModuleEnum ) );
                        }
                        if ( settings.CommentCountThreshold.Enabled && vid.Statistics.CommentCount <= (ulong) Math.Abs( settings.CommentCountThreshold.Value ) ) { //TODO Fix this math.abs nonsense with some validation
                            scores.Add( new AnalysisScore( commentCountScore, "Number of comments is below threshold", "Low comments", ModuleEnum ) );
                        }
                        if ( settings.NegativeVoteRatio.Enabled && vid.Statistics.DislikeCount > vid.Statistics.LikeCount ) {
                            scores.Add( new AnalysisScore( negativeVoteScore, "More dislikes than likes on video", ">50% dislikes", ModuleEnum ) );
                        }
                        if ( settings.VoteCountThreshold.Enabled && vid.Statistics.DislikeCount + vid.Statistics.LikeCount <= (ulong) Math.Abs( settings.VoteCountThreshold.Value ) ) { //TODO Fix this math.abs nonsense with some validation
                            scores.Add( new AnalysisScore( totalVotesScore, "Total vote count is below threshold", "Low Total Votes", ModuleEnum ) );
                        }
                        DateTime authorCreated= DateTime.UtcNow;
                        bool shadowbanned = false;
                        try {
                            authorCreated = post.Author.Created;
                        }
                        catch(WebException ex) {
                            if( ( ex.Response as HttpWebResponse ).StatusCode == HttpStatusCode.NotFound ) {
                                authorCreated = DateTime.UtcNow;
                                shadowbanned = true;
                            }
                            else {
                                throw;
                            }
                        }
                        if ( settings.RedditAccountAgeThreshold.Enabled && authorCreated.AddDays( settings.RedditAccountAgeThreshold.Value ) >= DateTime.UtcNow ) {
                            scores.Add( new AnalysisScore( redditAccountAgeScore, "Reddit Account age is below threshold", "New Reddit Acct", ModuleEnum ) );
                        }
                        if ( settings.ImgurSubmissionRatio.Enabled && !shadowbanned && ( (double) 100 / post.Author.Posts.Take( 100 ).Count( p => p.Domain.ToLower().Contains( "imgur" ) ) ) * 100 >= settings.ImgurSubmissionRatio.Value ) {
                            scores.Add( new AnalysisScore( imgurSubmissionRatioScore, "User has Imgur submissions above threshold for last 100 posts", "Lots of Imgur", ModuleEnum ) );
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
                                toReturn[post.Id].Scores.Add( new AnalysisScore( chanAgeScore, "Channel Age Below Threshold", "Channel Age", ModuleEnum ) );
                            }
                        }
                    }
                }

            }
           
            return toReturn;

        }
    }
}
