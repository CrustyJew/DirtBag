using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DirtBag.Helpers;
using DirtBag.Logging;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using RedditSharp;
using RedditSharp.Things;
using DirtBag.Models;

namespace DirtBag.Modules {
    class UserStalker {
        public string ModuleName { get { return "UserStalker"; } }
        public Modules ModuleEnum { get { return Modules.UserStalker; } }
        public bool MultiScan { get { return false; } }

        public IModuleSettings Settings { get; set; }
        public Reddit RedditClient { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }
        
        private Dictionary<string, int> processedCache;
        public UserStalker() {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
            processedCache = new Dictionary<string, int>();
            InitDatabase();
        }
        public UserStalker( UserStalkerSettings settings, Reddit reddit, string sub ) : this() {
            RedditClient = reddit;
            Subreddit = sub;
            Settings = settings;
        }

        public async Task<Dictionary<string, AnalysisDetails>> Analyze( List<Post> posts ) {
            //return await Task.Run( () => {
            var modLog = ProcessModLog();
            var toReturn = new Dictionary<string, AnalysisDetails>();
            var youTubePosts = new Dictionary<string, List<Post>>();
            foreach ( var post in posts ) {
                toReturn.Add( post.Id, new AnalysisDetails( post.Id, ModuleEnum ) );
                var ytID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );

                if ( !string.IsNullOrEmpty( ytID ) ) {
                    if ( !youTubePosts.ContainsKey( ytID ) ) youTubePosts.Add( ytID, new List<Post>() );
                    youTubePosts[ytID].Add( post );
                }
            }
            var yt = new YouTubeService( new BaseClientService.Initializer { ApiKey = YouTubeAPIKey } );

            var req = yt.Videos.List( "snippet" );
            for ( var i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                var response = await req.ExecuteAsync();

                foreach ( var vid in response.Items ) {
                    foreach ( var post in youTubePosts[vid.Id] ) {
                        UserPost.InsertPost( new UserPost { ChannelID = vid.Snippet.ChannelId, ChannelName = vid.Snippet.ChannelTitle, ThingID = post.Id, Link = post.Url.ToString(), PostTime = post.CreatedUTC, UserName = post.AuthorName, Subreddit = post.SubredditName } );
                    }
                }
            }
            //Task.Run( () => {


            //} );
            await modLog;

            return toReturn;
            //} );
        }
        private async Task ProcessModLog() {
            var yt = new YouTubeService( new BaseClientService.Initializer { ApiKey = YouTubeAPIKey } );

            var req = yt.Videos.List( "snippet" );
            var lastRemoval = PostRemoval.GetLastProcessedRemovalDate( Subreddit );
            var processedCount = 0;
            var modActions = RedditClient.GetSubreddit( Subreddit ).GetModerationLog( ModActionType.RemoveLink ).GetListing( 300, 100 );
            var newPosts = new Dictionary<string, List<UserPost>>();
            foreach ( var modAct in modActions ) {
                if ( modAct.TimeStamp <= lastRemoval || processedCount > 100 ) break; //probably dumb and unnecessary

                processedCount++;
                var post = RedditClient.GetThingByFullname( modAct.TargetThingFullname ) as Post;

                var userPost = new UserPost();
                userPost.ThingID = post.Id;
                userPost.Link = post.Url.ToString();
                userPost.PostTime = post.CreatedUTC;
                userPost.UserName = post.AuthorName;
                userPost.Subreddit = Subreddit;

                var removal = new PostRemoval( modAct );
                removal.Post = userPost;

                var newPost = PostRemoval.AddRemoval( removal );
                if ( newPost != null ) {

                    var ytID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );
                    if ( !string.IsNullOrEmpty( ytID ) ) {
                        if ( !newPosts.ContainsKey( ytID ) ) newPosts.Add( ytID, new List<UserPost>() );
                        newPosts[ytID].Add( newPost );
                    }

                }

                if ( processedCount % 75 == 0 ) {
                    await UpdateChannels( newPosts, req );
                    newPosts.Clear();
                }
            }

            await UpdateChannels( newPosts, req );
        }

        public void InitDatabase() {
            //Database is denormalized due to the impractical constraints of ensuring users and channels are loaded before
            //loading a post or a removal for a user. Normalizing as it is right now may actually reduce performance
            //and would certainly introduce more issues than it is probably worth unless more info is tacked on to some of the 
            //categories later on.
            using ( var con = DirtBagConnection.GetConn() ) {
                //bool useLocalDB = DirtBagConnection.UseLocalDB;
                bool useLocalDB = false;
                var initTables = "" +
                    ( useLocalDB ?
                        "CREATE TABLE IF NOT EXISTS " :
                        "if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'UserPosts' ) Create table "
                    ) +
                    "[UserPosts]([PostID] INTEGER NOT NULL PRIMARY KEY " + ( useLocalDB ? "AUTOINCREMENT" : "IDENTITY" ) + ", " +
                    //"[UserID] INTEGER NOT NULL, " +
                    "[UserName] nvarchar(50), " +
                    "[ThingID] varchar(10), " +
                    "[Link] nvarchar(200), " +
                    "[PostTime] DATETIME, " +
                    "[ChannelID] varchar(100), " +
                    "[ChannelName] nvarchar(200), " +
                    "[Subreddit] Nvarchar(100) ); " +
                    "" +
                    ( useLocalDB ?
                        "CREATE TABLE IF NOT EXISTS " :
                        "if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'PostRemovals' ) Create table "
                    ) +
                    "[PostRemovals]([RemovalID] INTEGER NOT NULL PRIMARY KEY " + ( useLocalDB ? "AUTOINCREMENT" : "IDENTITY" ) + ", " +
                    "[TimeStamp] DATETIME, " +
                    "[PostID] INTEGER NOT NULL, " +
                    "[ModName] NVARCHAR(50), " +
                    "[Reason] NVARCHAR(200) ); " +
                    "" +
                    //"CREATE TABLE IF NOT EXISTS [Channels]( " +
                    //"[ChannelID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    //"[Identifier] VARCHAR(100) NOT NULL, " +
                    //"[Name] varchar(200) NOT NULL ); " +
                    "" +
                    //"CREATE TABLE IF NOT EXISTS [Users]( " +
                    //"[UserID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    //"[UserName] VARCHAR(50) ); " +
                    "";
                con.Execute( initTables );
            }
        }

        private async Task UpdateChannels( Dictionary<string, List<UserPost>> newPosts, VideosResource.ListRequest req ) {
            for ( var i = 0; i < newPosts.Keys.Count; i += 50 ) {
                req.Id = string.Join( ",", newPosts.Keys.Skip( i ).Take( 50 ) );
                var response = await req.ExecuteAsync();
                foreach ( var vid in response.Items ) {
                    foreach ( var upost in newPosts[vid.Id] ) {
                        upost.ChannelID = vid.Snippet.ChannelId;
                        upost.ChannelName = vid.Snippet.ChannelTitle;
                        UserPost.UpdatePost( upost );
                    }
                }
            }
        }

    }
    public class UserStalkerSettings : IModuleSettings {
        public bool Enabled { get; set; }

        public int EveryXRuns { get; set; }

        public PostType PostTypes { get; set; }

        public double ScoreMultiplier { get; set; }

        public void SetDefaultSettings() {
            Enabled = false;
            EveryXRuns = 1;
            PostTypes = PostType.New;
            ScoreMultiplier = 1;
        }
    }
}
