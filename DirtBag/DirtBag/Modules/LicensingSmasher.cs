﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DirtBag.Helpers;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RedditSharp;
using RedditSharp.Things;

namespace DirtBag.Modules {
    class LicensingSmasher : IModule {
        public string ModuleName { get { return "LicensingSmasher"; } }
        public Modules ModuleEnum { get { return Modules.LicensingSmasher; } }
        public bool MultiScan { get { return true; } }
        public IModuleSettings Settings { get; set; }
        public bool IsRunning { get; set; }
        public Reddit RedditClient { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }
        public List<string> TermsToMatch { get; set; }
        public Dictionary<string, string> KnownLicensers { get; set; }
        public Flair RemovalFlair { get; set; }
        
        public LicensingSmasher() {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
        }
        public LicensingSmasher( LicensingSmasherSettings settings, Reddit reddit, string sub ) : this() {
            RedditClient = reddit;
            Subreddit = sub;
            TermsToMatch = settings.MatchTerms.ToList();
            KnownLicensers = settings.KnownLicensers;
            Settings = settings;
            RemovalFlair = settings.RemovalFlair;
            TermMatching = new Regex( string.Join( "|", settings.MatchTerms ), RegexOptions.IgnoreCase );
            LicenserMatching = new Regex( "^" + string.Join( "$|^", settings.KnownLicensers.Keys ) + "$", RegexOptions.IgnoreCase );
        }
        private const int STRINGMATCH_SCORE = 3;
        private const int ATTRIBUTION_SCORE = 2;
        private const int ATTRIBUTION_MATCH_SCORE = 10; //can only have 1 attribution score so it can be > 10
        private static Regex VideoID = new Regex( @"(?:youtube\.com/(?:(?:watch|attribution_link)\?(?:.*(?:&|%3F|&amp;))?v(?:=|%3D)|embed/|v/)|youtu\.be/)([a-zA-Z0-9-_]{11})" );
        private static string YouTubeScrapeFormat = "https://youtube.com/watch?v={0}";

        private Regex TermMatching;
        private Regex LicenserMatching;
        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<Post> posts ) {

            var toReturn = new Dictionary<string, PostAnalysisResults>();
            var youTubePosts = new Dictionary<string, List<Post>>();

            foreach ( var post in posts ) {
                toReturn.Add( post.Id, new PostAnalysisResults( post, ModuleEnum ) );
                var ytID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );

                if ( !string.IsNullOrEmpty( ytID ) ) {
                    if ( !youTubePosts.ContainsKey( ytID ) ) youTubePosts.Add( ytID, new List<Post>() );
                    youTubePosts[ytID].Add( post );
                }

            }
            var yt = new YouTubeService( new BaseClientService.Initializer { ApiKey = YouTubeAPIKey } );

            var req = yt.Videos.List( "snippet" );
            for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                var ids = youTubePosts.Keys.Skip( i ).Take( 50 );
                req.Id = string.Join( ",", ids );

                var ytScrape = ScrapeYouTube( youTubePosts.Skip( i ).Take( 50 ).ToDictionary( p => p.Key, p => p.Value ), toReturn );
                var response = await req.ExecuteAsync();

                foreach ( var vid in response.Items ) {
                    var redditPosts = youTubePosts[vid.Id];
                    //var scores = toReturn[post.Id].Scores;

                    var termMatches = TermMatching.Matches( vid.Snippet.Description ).Cast<Match>().Select( m => m.Value ).ToList();
                    termMatches.AddRange( TermMatching.Matches( vid.Snippet.Title ).Cast<Match>().Select( m => m.Value ).ToList().Distinct() );
                    if ( termMatches.Count > 0 ) {
                        foreach ( var post in redditPosts ) {
                            toReturn[post.Id].Scores.Add( new AnalysisScore( STRINGMATCH_SCORE * Settings.ScoreMultiplier, "YouTube video title or description has the following term(s): " + string.Join( ", ", termMatches ), "Match: " + string.Join( ", ", termMatches ), ModuleName, RemovalFlair ) );
                        }
                    }

                }
                await ytScrape;
            }

            return toReturn;
        }

        private async Task ScrapeYouTube( Dictionary<string, List<Post>> ytPosts, Dictionary<string, PostAnalysisResults> results ) {
            var scrapes = new Dictionary<string, Task<string>>();
            foreach ( var id in ytPosts.Keys ) {
                var c = new HttpClient();
                scrapes.Add( id, c.GetStringAsync( string.Format( YouTubeScrapeFormat, id ) ) );
            }

            while ( scrapes.Count > 0 ) {
                var scrape = await Task.WhenAny( scrapes.Values );
                var scrapeBody = await scrape;
                var dictItem = scrapes.First( i => i.Value == scrape );
                scrapes.Remove( dictItem.Key );

                var score = ScoreYouTubeMetaData( scrapeBody );
                if ( score != null ) {
                    foreach ( var post in ytPosts[dictItem.Key] ) {
                        if ( score.Score == 10 ) {
                            results[post.Id].Scores.Clear();
                        }
                        results[post.Id].Scores.Add( score );
                    }
                }
            }
        }

        private AnalysisScore ScoreYouTubeMetaData( string pageHtml ) {
            var doc = new HtmlDocument();
            doc.LoadHtml( pageHtml );
            var nodes = doc.DocumentNode.SelectNodes( "/html/head/meta[@name=\"attribution\"]" );
            AnalysisScore score = null;
            if ( nodes != null && nodes.Count > 0 ) {
                var node = nodes.First();
                var owner = node.GetAttributeValue( "content", "" );
                if ( owner.Substring( owner.Length - 1 ) == "/" ) owner = owner.Substring( 0, owner.Length - 1 );
                var match = LicenserMatching.Match( owner ).Value;
                score = new AnalysisScore( ATTRIBUTION_SCORE * Settings.ScoreMultiplier, string.Format( "Video is monetized by '{0}'", owner ), "Monetized", ModuleName );
                if ( !string.IsNullOrEmpty( match ) ) {
                    score = new AnalysisScore( ATTRIBUTION_MATCH_SCORE * Settings.ScoreMultiplier, string.Format( "Video is licensed through a network : '{0}'", KnownLicensers[match] ), string.Format( "Video licensed by '{0}'", KnownLicensers[match] ), ModuleName, RemovalFlair );
                    return score;
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
        public Flair RemovalFlair { get; set; }
        [JsonProperty]
        public string[] MatchTerms { get; set; }
        [JsonProperty]
        public Dictionary<string, string> KnownLicensers { get; set; }

        public double ScoreMultiplier { get; set; }

        public LicensingSmasherSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            PostTypes = PostType.All;
            EveryXRuns = 1;
            ScoreMultiplier = 1;
            MatchTerms = new[] { "jukin", "licensing", "break.com", "storyful", "rumble", "newsflare", "visualdesk", "viral spiral", "viralspiral", "rightser", "to use this video in a commercial", "media enquiries" };
            //These are case sensitive for friendly name matching
            KnownLicensers = new Dictionary<string, string> { { "H7XeNNPkVV3JZxXm-O-MCA", "Jukin Media" }, { "Newsflare", "Newsflare" }, { "3339WgBDKIcxTfywuSmG8w", "ViralHog" }, { "Storyful", "Storyful" }, { "rumble", "Rumble" }, { "Rightster_Entertainment_Affillia", "Viral Spiral" }, { "Break", "Break" } };
            RemovalFlair = new Flair() { Class = "red", Priority = 1, Text = "R10" };
        }
    }
}
