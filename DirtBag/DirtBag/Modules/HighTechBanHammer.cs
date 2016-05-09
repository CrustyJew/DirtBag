using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirtBag.Helpers;
using System.Configuration;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using rs = RedditSharp.Things;

namespace DirtBag.Modules {
    class HighTechBanHammer : IModule {
        private static RedditSharp.Things.Subreddit Subreddit;

        public Modules ModuleEnum { get { return Modules.HighTechBanHammer; } }
        public string ModuleName { get { return "HighTechBanHammer"; } }
        public bool MultiScan { get { return false; } }
        public IModuleSettings Settings { get; set; }

        public string YouTubeAPIKey { get; set; }

        public HighTechBanHammer() {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
        }

        public HighTechBanHammer( HighTechBanHammerSettings sets, RedditSharp.Things.Subreddit subreddit ) : this() {
            Settings = sets;
            Subreddit = subreddit;
        }

        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<rs.Post> posts ) {
            var toReturn = new Dictionary<string, PostAnalysisResults>();
            var youTubePosts = new Dictionary<string, List<rs.Post>>();
            var amWrangler = new BotFunctions.AutoModWrangler( Program.Client.GetSubreddit( Program.Subreddit ) );
            var bannedChannels = amWrangler.GetBannedList( Models.BannedEntity.EntityType.Channel );
            foreach ( var post in posts ) { //TODO error handling
                toReturn.Add( post.Id, new PostAnalysisResults( post, ModuleEnum ) );
                string postYTID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );

                if ( !string.IsNullOrWhiteSpace( postYTID ) ) {
                    if ( !youTubePosts.ContainsKey( postYTID ) ) youTubePosts.Add( postYTID, new List<rs.Post>() );
                    youTubePosts[postYTID].Add( post );
                }
            }
            var yt = new YouTubeService( new BaseClientService.Initializer { ApiKey = YouTubeAPIKey } );
            var req = yt.Videos.List( "snippet" );
            for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                var response = req.ExecuteAsync();
                await Task.WhenAll( response, bannedChannels );
                foreach ( var vid in response.Result.Items ) {
                    //if the channel is banned
                    var chan = bannedChannels.Result.Where( c => c.EntityString == vid.Snippet.ChannelId ).FirstOrDefault();
                    if ( chan != null ) {
                        foreach ( var ytPost in youTubePosts[vid.Id] ) {
                            //ring 'er up
                            toReturn[ytPost.Id].Scores.Add( new AnalysisScore( 9999, $"Channel ID: {chan.EntityString} was banned by {chan.BannedBy} on {chan.BanDate} for reason: {chan.BanReason}", "Banned Channel", ModuleName ) );
                        }
                    }
                }
            }
            return toReturn;
        }
    }

    public class HighTechBanHammerSettings : IModuleSettings {
        public bool Enabled { get; set; }
        public int EveryXRuns { get; set; }
        public PostType PostTypes { get; set; }     
        public double ScoreMultiplier { get; set; }

        public HighTechBanHammerSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = true;
            EveryXRuns = 1;
            PostTypes = PostType.New;
            ScoreMultiplier = 99;
        }
    }
}
