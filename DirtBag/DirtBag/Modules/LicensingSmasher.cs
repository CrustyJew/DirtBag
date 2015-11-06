using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DirtBag.Modules {
    class LicensingSmasher : IModule {
        public string ModuleName { get { return "LicensingSmasher"; } }
        public IModuleSettings Settings { get; set; }
        public bool IsRunning { get; set; }
        public RedditSharp.Reddit RedditClient { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }
        public List<string> TermsToMatch { get; set; }
        public LicensingSmasher() {
            string key = System.Configuration.ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
        }
        public LicensingSmasher( LicensingSmasherSettings settings, RedditSharp.Reddit reddit, string sub ) : this() {
            RedditClient = reddit;
            Subreddit = sub;
            TermsToMatch = settings.MatchTerms.ToList();
            Settings = settings;
            TermMatching = new Regex( string.Join( "|", settings.MatchTerms ), RegexOptions.IgnoreCase );
        }
        private const int STRINGMATCH_SCORE = 10;
        private static Regex VideoID = new Regex( @"(?:youtube\.com/(?:(?:watch|attribution_link)\?(?:.*(?:&|%3F|&amp;))?v(?:=|%3D)|embed/|v/)|youtu\.be/)([a-zA-Z0-9-_]{11})" );
        private Regex TermMatching;
        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<RedditSharp.Things.Post> posts ) {
            return await Task.Run( () => {
                Dictionary<string, PostAnalysisResults> toReturn = new Dictionary<string, PostAnalysisResults>();
                Dictionary<string, RedditSharp.Things.Post> youTubePosts = new Dictionary<string, RedditSharp.Things.Post>();
                foreach ( RedditSharp.Things.Post post in posts ) {
                    toReturn.Add( post.Id, new PostAnalysisResults( post ) );
                    if ( post.Url.Host.ToLower().Contains( "youtube" ) || post.Url.Host.ToLower().Contains( "youtu.bu" ) ) {
                        //it's a YouTube link
                        string url = post.Url.ToString();
                        if ( url.Contains( "v=" ) ) {
                            string id = url.Substring( url.IndexOf( "v=" ) + 2 ).Split( '&' )[0];
                            if ( !string.IsNullOrEmpty( id ) ) {
                                youTubePosts[id] = post;
                            }
                        }
                    }
                }
                Google.Apis.YouTube.v3.YouTubeService yt = new YouTubeService( new Google.Apis.Services.BaseClientService.Initializer() { ApiKey = YouTubeAPIKey } );

                var req = yt.Videos.List( "snippet,contentDetails,statistics" );
                for ( int i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                    req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                    var response = req.Execute();

                    foreach ( var vid in response.Items ) {
                        RedditSharp.Things.Post post = youTubePosts[vid.Id];
                        var scores = toReturn[post.Id].Scores;
                        
                        List<string> termMatches = TermMatching.Matches( vid.Snippet.Description ).Cast<Match>().Select( m => m.Value ).ToList();
                        termMatches.AddRange( TermMatching.Matches( vid.Snippet.Title ).Cast<Match>().Select( m => m.Value ).ToList().Distinct() );
                        if ( termMatches.Count > 0 ) {
                            scores.Add( new AnalysisScore( STRINGMATCH_SCORE * Settings.ScoreMultiplier, "YouTube video title or description has the following term(s): " + string.Join( ", ", termMatches ), "Match: " + string.Join( ", ", termMatches ), ModuleName ) );
                        }

                    }
                }

                return toReturn;
            } );
        }
    }

    public class LicensingSmasherSettings : IModuleSettings {
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonConverter( typeof( StringEnumConverter ) )]
        [JsonProperty]
        public PostType PostTypes { get; set; }
        [JsonProperty]
        public int EveryXRuns { get; set; }
        [JsonProperty]
        public string[] MatchTerms { get; set; }

        public double ScoreMultiplier { get; set; }

        public LicensingSmasherSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            PostTypes = PostType.All;
            EveryXRuns = 1;
            ScoreMultiplier = 1;
            MatchTerms = new string[] { "jukin", "licensing", "break.com", "storyful", "rumble", "newsflare", "visualdesk", "viral spiral", "viralspiral", "rightser", "to use this video in a commercial", "media enquiries" };
        }
    }
}
