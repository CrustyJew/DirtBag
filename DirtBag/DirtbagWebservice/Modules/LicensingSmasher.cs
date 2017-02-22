using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RedditSharp;
using RedditSharp.Things;
using DirtBagWebservice.Models;
using Microsoft.Extensions.Configuration;

namespace DirtBagWebservice.Modules {
    class LicensingSmasher : IModule {
        public string ModuleName { get { return "LicensingSmasher"; } }
        public Modules ModuleEnum { get { return Modules.LicensingSmasher; } }
        public bool MultiScan { get { return true; } }
        public IModuleSettings Settings { get; set; }
        public bool IsRunning { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }
        public List<string> TermsToMatch { get; set; }
        public Dictionary<string, string> KnownLicensers { get; set; }
        public Flair RemovalFlair { get; set; }
        
        public LicensingSmasher(IConfigurationRoot config) {
            var key = config["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
        }
        public LicensingSmasher( IConfigurationRoot config, LicensingSmasherSettings settings, string sub ) : this(config) {
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

        public async Task<AnalysisDetails> Analyze(AnalysisRequest request) {
            var results = await Analyze(new List<AnalysisRequest>() { request });
            return results.Values.FirstOrDefault();
        }

        public async Task<Dictionary<string, AnalysisDetails>> Analyze( List<AnalysisRequest> requests ) {

            var toReturn = new Dictionary<string, AnalysisDetails>();
            var youTubePosts = new Dictionary<string, List<string>>();

            foreach ( var request in requests ) {
                if ( toReturn.ContainsKey( request.ThingID ) ) {
                    continue; 
                }
                toReturn.Add( request.ThingID, new AnalysisDetails( request.ThingID, ModuleEnum ) );

                if ( !string.IsNullOrEmpty( request.MediaID ) && request.MediaPlatform == VideoProvider.YouTube ) {
                    if ( !youTubePosts.ContainsKey( request.MediaID ) ) youTubePosts.Add( request.MediaID, new List<string>() );
                    youTubePosts[request.MediaID].Add( request.ThingID );
                }
                else {
                    toReturn[request.ThingID].Scores.Add( new AnalysisScore( 0, $"{request.MediaPlatform} is unsupported", "", Modules.LicensingSmasher ) );
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
                    var redditThings = youTubePosts[vid.Id];
                    //var scores = toReturn[post.Id].Scores;

                    var termMatches = TermMatching.Matches( vid.Snippet.Description ).Cast<Match>().Select( m => m.Value ).ToList();
                    termMatches.AddRange( TermMatching.Matches( vid.Snippet.Title ).Cast<Match>().Select( m => m.Value ).ToList().Distinct() );
                    if ( termMatches.Count > 0 ) {
                        foreach ( var thingID in redditThings ) {
                            toReturn[thingID].Scores.Add( new AnalysisScore( STRINGMATCH_SCORE * Settings.ScoreMultiplier, "YouTube video title or description has the following term(s): " + string.Join( ", ", termMatches ), "Match: " + string.Join( ", ", termMatches ), ModuleEnum, RemovalFlair ) );
                        }
                    }

                }
                await ytScrape;
            }

            return toReturn;
        }

        private async Task ScrapeYouTube( Dictionary<string, List<string>> ytPosts, Dictionary<string, AnalysisDetails> results ) {
            var scrapes = new Dictionary<string, Task<string>>();
            foreach ( var id in ytPosts.Keys ) {
                var c = new HttpClient();
                scrapes.Add( id, c.GetStringAsync( string.Format( YouTubeScrapeFormat, id ) ) );
            }

            while ( scrapes.Count > 0 ) {
                var scrape = await Task.WhenAny( scrapes.Values ).ConfigureAwait(false);
                var scrapeBody = await scrape;
                var dictItem = scrapes.First( i => i.Value == scrape );
                scrapes.Remove( dictItem.Key );

                var score = ScoreYouTubeMetaData( scrapeBody );
                if ( score != null ) {
                    foreach ( var thingID in ytPosts[dictItem.Key] ) {
                        if ( score.Score == 10 ) {
                            results[thingID].Scores.Clear();
                        }
                        results[thingID].Scores.Add( score );
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
                score = new AnalysisScore( ATTRIBUTION_SCORE * Settings.ScoreMultiplier, string.Format( "Video is monetized by '{0}'", owner ), "Monetized", ModuleEnum );
                if ( !string.IsNullOrEmpty( match ) ) {
                    score = new AnalysisScore( ATTRIBUTION_MATCH_SCORE * Settings.ScoreMultiplier, string.Format( "Video is licensed through a network : '{0}'", KnownLicensers[match] ), string.Format( "Video licensed by '{0}'", KnownLicensers[match] ), ModuleEnum, RemovalFlair );
                    return score;
                }

            }
            return score;
        }
    }
}
