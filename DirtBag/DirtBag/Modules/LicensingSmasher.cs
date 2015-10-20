using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DirtBag.Modules {
    class LicensingSmasher {
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
        private const int ISLICENESED_SCORE = 2;
        private const int STRINGMATCH_SCORE = 8;
        private static Regex VideoID = new Regex( @"?: youtube\.com/(?:(?:watch|attribution_link)\?(?:.*(?:&|%3F|&amp;))?v(?:=|%3D)|embed/)|youtu\.be/)([a-zA-Z0-9-_]{11}" );
        public async Task<Dictionary<string, int>> Analyze( List<RedditSharp.Things.Post> posts ) {
            return await Task.Run( () => {
                Dictionary<string, int> toReturn = new Dictionary<string, int>();
                Dictionary<string, RedditSharp.Things.Post> youTubePosts = new Dictionary<string, RedditSharp.Things.Post>();
                foreach ( RedditSharp.Things.Post post in posts ) {
                    if ( post.Url.Host.ToLower().Contains( "youtube" ) || post.Url.Host.ToLower().Contains( "youtu.bu" ) ) {
                        //it's a YouTube vid
                        string id = VideoID.Match( post.Url.ToString() ).Value;
                        youTubePosts[id] = post;
                    }
                }
                Google.Apis.YouTube.v3.YouTubeService yt = new YouTubeService();
                var req = yt.Videos.List( "snippet,contentDetails" );
                req.Id = string.Join( ",", youTubePosts.Keys );
                var response = req.Execute();

                foreach ( var vid in response.Items ) {
                    int score = 0;
                    if ( vid.ContentDetails.LicensedContent.Value ) score += ISLICENESED_SCORE;
                    if ( TermsToMatch.Any( t => vid.Snippet.Description.IndexOf( t, StringComparison.InvariantCultureIgnoreCase ) >= 0 )
                         || TermsToMatch.Any( t => vid.Snippet.Title.IndexOf( t, StringComparison.InvariantCultureIgnoreCase ) >= 0 ) ) {
                        score += STRINGMATCH_SCORE;
                    }
                    RedditSharp.Things.Post post = youTubePosts[vid.Id];
                    toReturn[post.Id] = score;
                }

                return toReturn;
            } );
        }
    }

    public class LicensingSmasherSettings : IModuleSettings {
        [JsonProperty]
        public bool Enabled { get; set; }
        [JsonConverter( typeof( PostTypeConverter ) )]
        [JsonProperty]
        public PostType PostTypes { get; set; }
        [JsonProperty]
        public int EveryXRuns { get; set; }
        [JsonProperty]
        public string[] MatchTerms { get; set; }

        public LicensingSmasherSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = true;
            PostTypes = PostType.All;
            EveryXRuns = 1;
            MatchTerms = new string[] { "jukin", "licensing", "break.com", "storyful", "rumble", "newsflare", "visualdesk", "viral spiral", "viralspiral", "rightser", "to use this video in a commercial", "media enquiries" };
        }
    }
}
