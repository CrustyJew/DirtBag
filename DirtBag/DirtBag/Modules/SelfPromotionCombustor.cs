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

namespace DirtBag.Modules {
    class SelfPromotionCombustor : IModule {
        public string ModuleName {
            get {
                return "Self Promotion Combustor";
            }
        }

        public IModuleSettings Settings { get; set; }
        public RedditSharp.Reddit RedditClient { get; set; }
        public string YouTubeAPIKey { get; set; }
        public Flair RemovalFlair { get; set; }
        public int PercentageThreshold { get; set; }
        public bool IncludePostInPercentage { get; set; }
        private Dictionary<string, int> processedCache;
        private const int OVER_PERCENT_SCORE = 10;
        public SelfPromotionCombustor() {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
            processedCache = new Dictionary<string, int>();
        }

        public SelfPromotionCombustor( SelfPromotionCombustorSettings settings, RedditSharp.Reddit client ) : this() {
            RedditClient = client;
            Settings = settings;
            PercentageThreshold = settings.PercentageThreshold;
            IncludePostInPercentage = settings.IncludePostInPercentage;
            RemovalFlair = settings.RemovalFlair;
        }

        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<Post> posts ) {
            var toReturn = new Dictionary<string, PostAnalysisResults>();
            var unseenPosts = posts.Where( p => !processedCache.Keys.Contains( p.Id ) ).ToList();
            //called here after .ToList() so it marks them incase the process runs long (will run long on first start up)
            ManageCache( posts );
            foreach ( var post in unseenPosts ) { //TODO error handling
                var youTubePosts = new Dictionary<string, List<Post>>();

                toReturn.Add( post.Id, new PostAnalysisResults( post ) );
                string postYTID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );
                Task<Logging.UserPostingHistory> hist;
                if ( !string.IsNullOrEmpty( postYTID ) ) {
                    //It's a YouTube vid so we can kick off the analysis and get cookin
                    hist = Logging.UserPostingHistory.GetUserPostingHistory( post.AuthorName );
                    if ( !youTubePosts.ContainsKey( postYTID ) ) youTubePosts.Add( postYTID, new List<Post>() );
                    youTubePosts[postYTID].Add( post );
                }
                else {
                    //not a YouTube post, so bail out
                    continue;
                }
                bool success = false;
                int tries = 0;
                while ( !success && tries < 3 ) {
                    success = true;
                    try {
                        var recentPosts = RedditClient.Search<RedditSharp.Things.Post>( $"author:{post.AuthorName} self:no", RedditSharp.Sorting.New ).GetListing( 100, 100 );
                        foreach ( var recentPost in recentPosts ) {
                            string ytID = YouTubeHelpers.ExtractVideoId( recentPost.Url.ToString() );

                            if ( !string.IsNullOrEmpty( ytID ) ) {
                                if ( !youTubePosts.ContainsKey( ytID ) ) youTubePosts.Add( ytID, new List<Post>() );
                                youTubePosts[ytID].Add( post );
                            }
                        }
                    }
                    catch(Exception ex) {
                        success = false;
                        tries++;
                        if(tries > 3 ) {
                            Console.WriteLine( $"Failed to get search results: {ex.Message}" );
                            processedCache.Remove( post.Id );
                            break; 
                        }
                        await Task.Delay( 100 );
                    }
                }
                if ( tries > 3 ) {
                    continue;
                }
                var yt = new YouTubeService( new BaseClientService.Initializer { ApiKey = YouTubeAPIKey } );

                Dictionary<string, List<string>> postHistory = ( await hist ).PostingHistory;
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
                            if ( !postHistory[vid.Snippet.ChannelId].Contains( ytPost.Id ) ) postHistory[vid.Snippet.ChannelId].Add( ytPost.Id );

                            if ( vid.Id == postYTID ) {
                                postChannelID = vid.Snippet.ChannelId;
                                postChannelName = vid.Snippet.ChannelTitle;
                            }
                        }
                    }
                }

                if ( string.IsNullOrEmpty( postChannelID ) ) {
                    //shouldn't ever happen, but might if the video is deleted or the channel deleted or something
                    Console.WriteLine( $"Channel for post {post.Id} by {post.AuthorName} couldn't be found" );
                    continue;
                }

                int totalPosts = postHistory.Sum( ph => ph.Value.Count );
                int channelPosts = postHistory[postChannelID].Count;
                if ( !IncludePostInPercentage ) {
                    totalPosts--;
                    channelPosts--;
                    postHistory[postChannelID].Remove( post.Id );
                }
                double percent = ( (double) channelPosts / totalPosts ) * 100;
                if ( percent > PercentageThreshold ) {
                    var score = new AnalysisScore();
                    score.ModuleName = "SelfPromotionCombustor";
                    score.ReportReason = $"SelfPromo: {Math.Round( percent, 2 )}%";
                    score.Reason = $"Self Promotion for channel '{postChannelName}' with a posting percentage of {Math.Round( percent, 2 )}. Found PostIDs: {string.Join( ", ", postHistory[postChannelID] )}";
                    score.Score = OVER_PERCENT_SCORE;
                    score.RemovalFlair = RemovalFlair;

                    toReturn[post.Id].Scores.Add( score );
                }
            }
            return toReturn;
        }

        private void ManageCache( List<Post> posts ) {

            IEnumerable<string> postIDs = posts.Select( p => p.Id );
            foreach ( var notSeen in processedCache.Where( c => !postIDs.Contains( c.Key ) ).ToArray() ) {
                processedCache[notSeen.Key]++;
            }
            foreach ( string id in postIDs ) {
                if ( processedCache.ContainsKey( id ) ) processedCache[id] = 0;
                else processedCache.Add( id, 0 );
            }
            foreach ( var expired in processedCache.Where( c => c.Value > 3 ).ToArray() ) {
                processedCache.Remove( expired.Key );
            }

        }

    }

    public class SelfPromotionCombustorSettings : IModuleSettings {
        public bool Enabled { get; set; }

        public int EveryXRuns { get; set; }

        public PostType PostTypes { get; set; }

        [JsonProperty]
        public Flair RemovalFlair { get; set; }

        public double ScoreMultiplier { get; set; }
        [JsonProperty]
        public int PercentageThreshold { get; set; }
        [JsonProperty]
        public bool IncludePostInPercentage { get; set; }

        public SelfPromotionCombustorSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            EveryXRuns = 1;
            PostTypes = PostType.New;
            ScoreMultiplier = 1;
            PercentageThreshold = 10;
            RemovalFlair = new Flair( "10%", "red", 1 );
            IncludePostInPercentage = false;
        }
    }
}
