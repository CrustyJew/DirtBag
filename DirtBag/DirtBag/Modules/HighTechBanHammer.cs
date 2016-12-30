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
using DirtBag.Models;

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

        public async Task<Dictionary<string, AnalysisDetails>> Analyze( List<AnalysisRequest> requests ) {
            var toReturn = new Dictionary<string, AnalysisDetails>();
            var youTubePosts = new Dictionary<string, List<string>>();
            var amWrangler = new BotFunctions.AutoModWrangler( Program.Client.GetSubreddit( Program.Subreddit ) );
            var bannedChannels = amWrangler.GetBannedList( Models.BannedEntity.EntityType.Channel );
            foreach ( var request in requests ) { //TODO error handling
                if ( toReturn.ContainsKey( request.ThingID ) ) {
                    continue;
                }
                toReturn.Add( request.ThingID, new AnalysisDetails( request.ThingID, ModuleEnum ) );
                

                if ( !string.IsNullOrWhiteSpace( request.VideoID ) ) {
                    if ( !youTubePosts.ContainsKey( request.VideoID ) ) youTubePosts.Add( request.VideoID, new List<string>() );
                    youTubePosts[request.VideoID].Add( request.ThingID );
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
                        foreach ( var thingID in youTubePosts[vid.Id] ) {
                            //ring 'er up
                            toReturn[thingID].Scores.Add( new AnalysisScore( 9999, $"Channel ID: {chan.EntityString} was banned by {chan.BannedBy} on {chan.BanDate} for reason: {chan.BanReason}", "Banned Channel", ModuleEnum ) );
                        }
                    }
                }
            }
            return toReturn;
        }
    }
}
