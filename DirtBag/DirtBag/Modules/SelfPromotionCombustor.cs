using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedditSharp.Things;
using DirtBag.Helpers;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Configuration;
using Newtonsoft.Json;
using DirtBag.Models;

namespace DirtBag.Modules {
    class SelfPromotionCombustor : IModule {
        public string ModuleName {
            get {
                return "Self Promotion Combustor";
            }
        }
        public Modules ModuleEnum { get { return Modules.SelfPromotionCombustor; } }
        public bool MultiScan { get { return false; } }

        public IModuleSettings Settings { get; set; }
        public string YouTubeAPIKey { get; set; }
        public Flair RemovalFlair { get; set; }
        public int PercentageThreshold { get; set; }
        public bool IncludePostInPercentage { get; set; }
        public int GracePeriod { get; set; }

        private DAL.UserPostingHistoryDAL _userPostHistDAL;

        private Dictionary<string, int> processedCache;
        private const int OVER_PERCENT_SCORE = 10;
        public SelfPromotionCombustor(DAL.UserPostingHistoryDAL postHistoryDAL) {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
            processedCache = new Dictionary<string, int>();
            _userPostHistDAL = postHistoryDAL;
        }

        public SelfPromotionCombustor( SelfPromotionCombustorSettings settings, DAL.UserPostingHistoryDAL postHistoryDAL ) : this(postHistoryDAL) {

            Settings = settings;
            PercentageThreshold = settings.PercentageThreshold;
            IncludePostInPercentage = settings.IncludePostInPercentage;
            RemovalFlair = settings.RemovalFlair;
            GracePeriod = settings.GracePeriod;
        }

        public async Task<AnalysisDetails> Analyze(AnalysisRequest request)
        {
            var results = await Analyze(new List<AnalysisRequest>() { request });
            return results.Values.FirstOrDefault();
        }
        public async Task<Dictionary<string, AnalysisDetails>> Analyze( List<AnalysisRequest> requests ) {
            var toReturn = new Dictionary<string, AnalysisDetails>();
            foreach ( var request in requests ) { //TODO error handling
                var youTubePosts = new Dictionary<string, List<string>>();

                toReturn.Add( request.ThingID, new AnalysisDetails( request.ThingID, ModuleEnum ) );
                
                Task<Dictionary<string,string>> hist;
                IEnumerable<Models.UserPostInfo> postHistory;
                if ( !string.IsNullOrEmpty( request.VideoID ) ) {
                    //It's a YouTube vid so we can kick off the analysis and get cookin
                    postHistory = await _userPostHistDAL.GetUserPostingHistoryAsync( request.Author.Name );
                    
                }
                else {
                    //not a YouTube post, so bail out
                    continue;
                }
                hist = new Task<Dictionary<string, string>>(()=>new Dictionary<string,string>());
                bool success = false;
                int nonYTPosts = 0;
                /*int tries = 0;
                while ( !success && tries < 3 ) {
                    success = true;
                    try {
                        var recentPosts = RedditClient.Search<RedditSharp.Things.Post>( $"author:{request.Author.Name} self:no", RedditSharp.Sorting.New ).GetListing( 100, 100 );
                        foreach ( var recentPost in recentPosts ) {
                            string ytID = YouTubeHelpers.ExtractVideoId( recentPost.Url.ToString() );

                            if ( !string.IsNullOrEmpty( ytID ) ) {
                                if ( !youTubePosts.ContainsKey( ytID ) ) youTubePosts.Add( ytID, new List<string>() );
                                youTubePosts[ytID].Add( request.ThingID );
                            }
                            else {
                                nonYTPosts++;
                            }
                        }
                    }
                    catch(Exception ex) {
                        success = false;
                        tries++;
                        if(tries > 3 ) {
                            Console.WriteLine( $"Failed to get search results: {ex.Message}" );
                            processedCache.Remove( request.ThingID);
                            break; 
                        }
                        await Task.Delay( 100 );
                    }
                }
                if ( tries > 3 ) {
                    continue;
                }*/
                /*var yt = new YouTubeService( new BaseClientService.Initializer { ApiKey = YouTubeAPIKey } );
                var userPosts = await hist;
                Dictionary<string, List<string>> postHistory = new Dictionary<string, List<string>>();
                foreach(var post in userPosts ) {
                    if ( !postHistory.ContainsKey( post.Value ) ) {
                        postHistory.Add( post.Value, new List<string>() );
                    }
                    postHistory[post.Value].Add( post.Key );
                }
                string postChannelID = "";
                string postChannelName = "";
                var req = yt.Videos.List( "snippet" );
                for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                    req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                    var response = await req.ExecuteAsync();

                    foreach ( var vid in response.Items ) {
                        foreach ( var ytPost in youTubePosts[vid.Id] ) {
                            if ( !postHistory.ContainsKey( vid.Snippet.ChannelId ) ) postHistory.Add( vid.Snippet.ChannelId, new List<string>() );
                            //check to see if it already exists (aka wasnt deleted and showed up in search results)
                            if ( !postHistory[vid.Snippet.ChannelId].Contains( ytPost ) ) postHistory[vid.Snippet.ChannelId].Add( ytPost );

                            if ( vid.Id == request.ThingID ) {
                                postChannelID = vid.Snippet.ChannelId;
                                postChannelName = vid.Snippet.ChannelTitle;
                            }
                        }
                    }
                }*/


                var yt = new YouTubeService(new BaseClientService.Initializer { ApiKey = YouTubeAPIKey });
                var req = yt.Videos.List("snippet");
                req.Id = request.VideoID;
                var ytResponse = (await req.ExecuteAsync()).Items.FirstOrDefault();

                if ( string.IsNullOrEmpty(ytResponse.Snippet.ChannelTitle) ) {
                    //shouldn't ever happen, but might if the video is deleted or the channel deleted or something
                    Console.WriteLine( $"Channel for thing {request.ThingID} by {request.Author.Name} couldn't be found" );
                    continue;
                }

                int totalPosts = postHistory.Count();
                int channelPosts = postHistory.Count(ph => ph.MediaChannelID == ytResponse.Snippet.ChannelTitle);
                if ( !IncludePostInPercentage ) {
                    totalPosts--;
                    channelPosts--;
                }
                double percent = ( (double) channelPosts / totalPosts ) * 100;
                if ( percent > PercentageThreshold && channelPosts > GracePeriod ) {
                    var score = new AnalysisScore();
                    score.Module = ModuleEnum;
                    score.ReportReason = $"SelfPromo: {Math.Round( percent, 2 )}%";
                    score.Reason = $"Self Promotion for channel '{ytResponse.Snippet.ChannelTitle}' with a posting percentage of {Math.Round( percent, 2 )}. Found PostIDs: {string.Join( ", ", postHistory.Select(ph=>ph.ThingID) )}";
                    score.Score = OVER_PERCENT_SCORE * Settings.ScoreMultiplier;
                    score.RemovalFlair = RemovalFlair;

                    toReturn[request.ThingID].Scores.Add( score );
                }
            }
            return toReturn;
        }
    }
}
