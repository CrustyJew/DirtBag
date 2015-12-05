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

namespace DirtBag.Modules {
    class UserStalker : IModule {
        public string ModuleName { get { return "UserStalker"; } }

        public IModuleSettings Settings { get; set; }
        public Reddit RedditClient { get; set; }
        public string Subreddit { get; set; }
        public string YouTubeAPIKey { get; set; }
        public UserStalker() {
            var key = ConfigurationManager.AppSettings["YouTubeAPIKey"];
            if ( string.IsNullOrEmpty( key ) ) throw new Exception( "Provide setting 'YouTubeAPIKey' in AppConfig" );
            YouTubeAPIKey = key;
            InitDatabase();
        }
        public UserStalker( UserStalkerSettings settings, Reddit reddit, string sub ) : this() {
            RedditClient = reddit;
            Subreddit = sub;
            Settings = settings;
        }

        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<Post> posts ) {
            return await Task.Run( () => {
                var toReturn = new Dictionary<string, PostAnalysisResults>();
                var youTubePosts = new Dictionary<string, List<Post>>();
                foreach ( var post in posts ) {
                    toReturn.Add( post.Id, new PostAnalysisResults( post ) );
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
                    var response = req.Execute();

                    foreach ( var vid in response.Items ) {
                        foreach ( var post in youTubePosts[vid.Id] ) {
                            UserPost.InsertPost( new UserPost { ChannelID = vid.Snippet.ChannelId, ChannelName = vid.Snippet.ChannelTitle, Link = post.Permalink.ToString(), UserName = post.AuthorName, Subreddit = post.SubredditName } );
                        }
                    }
                }
                Task.Run( () => {
                    var lastRemoval = PostRemoval.GetLastProcessedRemovalDate( Subreddit );
                    var processedCount = 0;
                    var modActions = RedditClient.GetSubreddit( Subreddit ).GetModerationLog( ModActionType.RemoveLink ).GetListing( 5000, 100 );
                    var newPosts = new Dictionary<string, List<UserPost>>();
                    foreach ( var modAct in modActions ) {
                        if ( modAct.TimeStamp <= lastRemoval || processedCount > 2500 ) break;

                        processedCount++;
                        var post = RedditClient.GetThingByFullname( modAct.TargetThingFullname ) as Post;

                        var userPost = new UserPost();
                        userPost.Link = post.Permalink.ToString();
                        userPost.UserName = post.AuthorName;
                        userPost.Subreddit = Subreddit;

                        var removal = new PostRemoval( modAct );
                        removal.Post = userPost;

                        var newPost = PostRemoval.AddRemoval( removal );
                        if ( newPost != null ) {
                            if ( post.Url.Host.ToLower().Contains( "youtube" ) || post.Url.Host.ToLower().Contains( "youtu.bu" ) ) {
                                var ytID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );
                                if ( !string.IsNullOrEmpty( ytID ) ) {
                                    if ( !newPosts.ContainsKey( ytID ) ) newPosts.Add( ytID, new List<UserPost>() );
                                    newPosts[ytID].Add( newPost );
                                }
                            }
                        }
                    }


                    for ( var i = 0; i < newPosts.Keys.Count; i += 50 ) {
                        req.Id = string.Join( ",", newPosts.Keys.Skip( i ).Take( 50 ) );
                        var response = req.Execute();
                        foreach ( var vid in response.Items ) {
                            foreach ( var upost in newPosts[vid.Id] ) {
                                upost.ChannelID = vid.Snippet.ChannelId;
                                upost.ChannelName = vid.Snippet.ChannelTitle;
                                UserPost.UpdatePost( upost );
                            }
                        }
                    }
                } );


                return toReturn;
            } );
        }
        public void InitDatabase() {
            //Database is denormalized due to the impractical constraints of ensuring users and channels are loaded before
            //loading a post or a removal for a user. Normalizing as it is right now may actually reduce performance
            //and would certainly into more issues than it is probably worth unless more info is tacked on to some of the 
            //categories later on.
            using ( var con = DirtBagConnection.GetConn() ) {
                var initTables = "" +
                    "CREATE TABLE IF NOT EXISTS [UserPosts]( " +
                    "[PostID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    //"[UserID] INTEGER NOT NULL, " +
                    "[UserName] varchar(50), " +
                    "[Link] varchar(200), " +
                    "[ChannelID] varchar(100), " +
                    "[ChannelName] varchar(200), " +
                    "[Subreddit] varchar(100) );" +
                    "" +
                    "CREATE TABLE IF NOT EXISTS [PostRemovals]( " +
                    "[RemovalID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    "[TimeStamp] DATETIME, " +
                    "[PostID] INTEGER NOT NULL, " +
                    "[ModName] VARCHAR(50), " +
                    "[Reason] VARCHAR(200) ); " +
                    "" +
                    //"CREATE TABLE IF NOT EXISTS [Channels]( " +
                    //"[ChannelID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    //"[Identifier] VARCHAR(100) NOT NULL, " +
                    //"[Name] varchar(200) NOT NULL ); " +
                    "" +
                    "CREATE TABLE IF NOT EXISTS [Users]( " +
                    "[UserID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    "[UserName] VARCHAR(50) ); " +
                    "";
                con.Execute( initTables );
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
