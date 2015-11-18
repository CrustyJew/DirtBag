using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using HtmlAgilityPack;
using System.Net.Http;

namespace DirtBag.Modules {
    class LicensingSmasher : IModule {
        public string ModuleName { get { return "LicensingSmasher"; } }
        public IModuleSettings Settings { get; set; }
        public bool IsRunning { get; set; }
        public RedditSharp.Reddit RedditClient { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }
        public List<string> TermsToMatch { get; set; }
        public List<string> KnownLicensers { get; set; }
        public LicensingSmasher() {
            string key = System.Configuration.ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
        }
        public LicensingSmasher( LicensingSmasherSettings settings, RedditSharp.Reddit reddit, string sub ) : this() {
            RedditClient = reddit;
            Subreddit = sub;
            TermsToMatch = settings.MatchTerms.ToList();
            KnownLicensers = settings.KnownLicensers.ToList();
            Settings = settings;
            TermMatching = new Regex( string.Join( "|", settings.MatchTerms ), RegexOptions.IgnoreCase );
            LicenserMatching = new Regex( string.Join( "|", settings.KnownLicensers ), RegexOptions.IgnoreCase );
        }
        private const int STRINGMATCH_SCORE = 3;
        private const int ATTRIBUTION_SCORE = 1;
        private const int ATTRIBUTION_MATCH_SCORE = 6;
        private static Regex VideoID = new Regex( @"(?:youtube\.com/(?:(?:watch|attribution_link)\?(?:.*(?:&|%3F|&amp;))?v(?:=|%3D)|embed/|v/)|youtu\.be/)([a-zA-Z0-9-_]{11})" );
        private static string YouTubeScrapeFormat = "https://youtu.be/{0}";

        private Regex TermMatching;
        private Regex LicenserMatching;
        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<RedditSharp.Things.Post> posts ) {
            return await Task.Run( async () => {
                Dictionary<string, PostAnalysisResults> toReturn = new Dictionary<string, PostAnalysisResults>();
                Dictionary<string, List<RedditSharp.Things.Post>> youTubePosts = new Dictionary<string, List<RedditSharp.Things.Post>>();
                foreach ( RedditSharp.Things.Post post in posts ) {
                    toReturn.Add( post.Id, new PostAnalysisResults( post ) );
                    if ( post.Url.Host.ToLower().Contains( "youtube" ) || post.Url.Host.ToLower().Contains( "youtu.bu" ) ) {
                        //it's a YouTube link
                        string url = post.Url.ToString();
                        if ( url.Contains( "v=" ) ) {
                            string id = url.Substring( url.IndexOf( "v=" ) + 2 ).Split( '&' )[0];
                            if ( !string.IsNullOrEmpty( id ) ) {
                                if ( !youTubePosts.ContainsKey( id ) ) youTubePosts.Add( id, new List<RedditSharp.Things.Post>() );
                                youTubePosts[id].Add( post );
                            }
                        }
                    }
                }
                Google.Apis.YouTube.v3.YouTubeService yt = new YouTubeService( new Google.Apis.Services.BaseClientService.Initializer() { ApiKey = YouTubeAPIKey } );

                var req = yt.Videos.List( "snippet" );
                for ( int i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                    IEnumerable<string> ids = youTubePosts.Keys.Skip( i ).Take( 50 );
                    req.Id = string.Join( ",", ids );

                    var ytScrape = ScrapeYouTube( youTubePosts.Skip( i ).Take( 50 ).ToDictionary( p => p.Key, p => p.Value ), toReturn );
                    var response = req.Execute();

                    foreach ( var vid in response.Items ) {
                        List<RedditSharp.Things.Post> redditPosts = youTubePosts[vid.Id];
                        //var scores = toReturn[post.Id].Scores;

                        List<string> termMatches = TermMatching.Matches( vid.Snippet.Description ).Cast<Match>().Select( m => m.Value ).ToList();
                        termMatches.AddRange( TermMatching.Matches( vid.Snippet.Title ).Cast<Match>().Select( m => m.Value ).ToList().Distinct() );
                        if ( termMatches.Count > 0 ) {
                            foreach ( var post in redditPosts ) {
                                toReturn[post.Id].Scores.Add( new AnalysisScore( STRINGMATCH_SCORE * Settings.ScoreMultiplier, "YouTube video title or description has the following term(s): " + string.Join( ", ", termMatches ), "Match: " + string.Join( ", ", termMatches ), ModuleName ) );
                            }
                        }

                    }
                    await ytScrape;
                }

                return toReturn;
            } );
        }

        private async Task ScrapeYouTube( Dictionary<string, List<RedditSharp.Things.Post>> ytPosts, Dictionary<string, PostAnalysisResults> results ) {
            Dictionary<string, Task<string>> scrapes = new Dictionary<string, Task<string>>();
            foreach ( string id in ytPosts.Keys ) {
                HttpClient c = new HttpClient();
                scrapes.Add( id, c.GetStringAsync( string.Format( YouTubeScrapeFormat, id ) ) );
            }

            while ( scrapes.Count > 0 ) {
                var scrape = await Task.WhenAny( scrapes.Values );
                string scrapeBody = await scrape;
                var dictItem = scrapes.First( i => i.Value == scrape );
                scrapes.Remove( dictItem.Key );

                var score = ScoreYouTubeMetaData( scrapeBody );
                if ( score != null ) {
                    foreach ( var post in ytPosts[dictItem.Key] ) {
                        results[post.Id].Scores.Add( score );
                    }
                }
            }
        }

        private AnalysisScore ScoreYouTubeMetaData( string pageHtml ) {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml( pageHtml );
            var nodes = doc.DocumentNode.SelectNodes( "/html/head/meta[@name=\"attribution\"]" );
            AnalysisScore score = null;
            if ( nodes != null && nodes.Count > 0 ) {
                foreach ( var node in nodes ) {
                    string owner = node.GetAttributeValue( "content", "" );
                    string match = LicenserMatching.Match( owner ).Value;
                    score = new AnalysisScore( ATTRIBUTION_SCORE * Settings.ScoreMultiplier, string.Format( "Video is monetized by '{0}'", owner ), string.Format( "Monetized by '{0}'", match ), ModuleName );
                    if ( !string.IsNullOrEmpty( match ) ) {
                        score = new AnalysisScore( ATTRIBUTION_MATCH_SCORE * Settings.ScoreMultiplier, string.Format( "Video is licensed through a network : '{0}'", match ), string.Format( "Video licensed by '{0}'", match ), ModuleName );
                        return score;
                    }
                }
            }
            return score;
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
        [JsonProperty]
        public string[] KnownLicensers { get; set; }

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
            KnownLicensers = new string[] { "H7XeNNPkVV3JZxXm-O-MCA", "Newsflare", "3339WgBDKIcxTfywuSmG8w", "viralhog", "Storyful", "rumble", "Rightster_Entertainment_Affillia", "Break", "FullScreen" };
        }
    }
}
