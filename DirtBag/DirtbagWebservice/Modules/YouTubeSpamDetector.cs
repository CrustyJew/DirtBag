using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;
using System.Net;
using DirtBagWebservice.Models;
using Microsoft.Extensions.Configuration;

namespace DirtBagWebservice.Modules {
    class YouTubeSpamDetector : IModule {
        public string ModuleName {
            get {
                return "YouTubeSpamDetector";
            }
        }
        public Modules ModuleEnum { get { return Modules.YouTubeSpamDetector; } }
        public bool MultiScan { get { return false; } }

        public IModuleSettings Settings { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }

        private const int MAX_MODULE_SCORE = 10;

        private Dictionary<string, int> processedCache;

        public YouTubeSpamDetector(IConfigurationRoot config) {
            var key = config["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
            processedCache = new Dictionary<string, int>();
        }

        public YouTubeSpamDetector( IConfigurationRoot config, YouTubeSpamDetectorSettings settings, string sub ) : this(config) {
            Settings = settings;
            Subreddit = sub;
            Settings = settings;
        }
        public async Task<AnalysisDetails> Analyze(AnalysisRequest request)
        {
            var results = await Analyze(new List<AnalysisRequest>() { request });
            return results.Values.FirstOrDefault();
        }
        public async Task<Dictionary<string, AnalysisDetails>> Analyze( List<AnalysisRequest> requests ) {

            var toReturn = new Dictionary<string, AnalysisDetails>();
            var youTubePosts = new Dictionary<string, List<AnalysisRequest>>();
            var channels = new Dictionary<string, List<AnalysisRequest>>();
            foreach ( var request in requests ) {
                toReturn.Add( request.ThingID, new AnalysisDetails( request.ThingID, ModuleEnum ) );
                if ( request.MediaPlatform == VideoProvider.YouTube ) {
                    if ( !string.IsNullOrEmpty( request.MediaID ) ) {
                        if ( !youTubePosts.ContainsKey( request.MediaID ) ) youTubePosts.Add( request.MediaID, new List<AnalysisRequest>() );
                        youTubePosts[request.MediaID].Add( request );
                    }
                    if ( !string.IsNullOrWhiteSpace( request.MediaChannelID ) ) {
                        if ( !channels.ContainsKey( request.MediaChannelID ) ) channels[request.MediaChannelID] = new List<AnalysisRequest>();
                        channels[request.MediaChannelID].Add( request );
                    }
                    if(string.IsNullOrWhiteSpace(request.MediaChannelID) && string.IsNullOrWhiteSpace( request.MediaID ) ) {
                        toReturn[request.ThingID].Scores.Add( new AnalysisScore( 0, $"Channel and media id are blank", "", Modules.YouTubeSpamDetector ) );
                    }
                }
                else {
                    toReturn[request.ThingID].Scores.Add( new AnalysisScore( 0, $"{request.MediaPlatform} is unsupported", "", Modules.YouTubeSpamDetector ) );
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
            availWeight += settings.CommentCountThreshold.Enabled ? settings.CommentCountThreshold.Weight : 0;
            availWeight += settings.VoteCountThreshold.Enabled ? settings.VoteCountThreshold.Weight : 0;
            availWeight += settings.ChannelSubscribersThreshold.Enabled ? settings.ChannelSubscribersThreshold.Weight : 0;

            var chanAgeScore = ( settings.ChannelAgeThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
            var viewCountScore = ( settings.ViewCountThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
            var negativeVoteScore = ( settings.NegativeVoteRatio.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
            var redditAccountAgeScore = ( settings.RedditAccountAgeThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
            var licensedScore = ( settings.LicensedChannel.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
            var commentCountScore = ( settings.CommentCountThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
            var totalVotesScore = ( settings.VoteCountThreshold.Weight / availWeight ) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
            var subscribersScore = (settings.ChannelSubscribersThreshold.Weight / availWeight) * MAX_MODULE_SCORE * Settings.ScoreMultiplier;
           
            for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                
                req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                var response = await req.ExecuteAsync();

                foreach ( var vid in response.Items ) {
                    foreach ( var analysisReq in youTubePosts[vid.Id] ) {
                        var scores = toReturn[analysisReq.ThingID].Scores;

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
                        
                        if ( settings.RedditAccountAgeThreshold.Enabled && analysisReq.Author.Created.HasValue && analysisReq.Author.Created.Value.AddDays( settings.RedditAccountAgeThreshold.Value ) >= DateTime.UtcNow ) {
                            scores.Add( new AnalysisScore( redditAccountAgeScore, "Reddit Account age is below threshold", "New Reddit Acct", ModuleEnum ) );
                        }
                        
                    }
                }
                

            }
            if ( settings.ChannelAgeThreshold.Enabled || settings.ChannelSubscribersThreshold.Enabled ) {
                for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                    var chanReq = yt.Channels.List( "snippet,statistics" );
                    chanReq.Id = string.Join( ",", channels.Keys.Skip(i).Take(50) );
                    var chanResponse = chanReq.Execute();
                    //get the channel info
                    foreach ( var channel in chanResponse.Items ) {
                        //if the channel was created less than the settings.ChannelAgeThreshold days ago
                        DateTime channelCreationDate = channel.Snippet.PublishedAt.HasValue ? channel.Snippet.PublishedAt.Value : DateTime.UtcNow;
                        long channelSubscribers = channel.Statistics.SubscriberCount.HasValue ? (long) channel.Statistics.SubscriberCount.Value : 0;
                        foreach ( var analysisReq in channels[channel.Id] ) {
                            if ( settings.ChannelAgeThreshold.Enabled && channelCreationDate.AddDays( settings.ChannelAgeThreshold.Value ) >= analysisReq.EntryTime ) {

                                //Add the score to the posts
                                toReturn[analysisReq.ThingID].Scores.Add( new AnalysisScore( chanAgeScore, "Channel Age Below Threshold", "Channel Age", ModuleEnum ) );
                            }
                            if ( settings.ChannelSubscribersThreshold.Enabled && channelSubscribers <= settings.ChannelSubscribersThreshold.Value ) {
                                toReturn[analysisReq.ThingID].Scores.Add( new AnalysisScore( subscribersScore, "Subscriber Count Below Threshold", "Num Subscribers", ModuleEnum ) );
                            }
                        }
                    }
                }
            }

            return toReturn;

        }
    }
}
