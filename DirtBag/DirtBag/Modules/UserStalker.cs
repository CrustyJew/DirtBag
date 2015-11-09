using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedditSharp.Things;
using System.Data.Common;
using Dapper;
using Google.Apis.YouTube.v3;

namespace DirtBag.Modules {
    class UserStalker : IModule {
        public string ModuleName { get { return "UserStalker"; } }

        public IModuleSettings Settings { get; set; }

        public async Task<Dictionary<string, PostAnalysisResults>> Analyze( List<Post> posts ) {
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

                var req = yt.Videos.List( "snippet" );
                for ( int i = 0; i < youTubePosts.Keys.Count; i += 50 ) {
                    req.Id = string.Join( ",", youTubePosts.Keys.Skip( i ).Take( 50 ) );
                    var response = req.Execute();

                    foreach ( var vid in response.Items ) {
                        RedditSharp.Things.Post post = youTubePosts[vid.Id];
                        var scores = toReturn[post.Id].Scores;
                        Logging.UserPost.InsertPost( new Logging.UserPost() { ChannelID = vid.Snippet.ChannelId, ChannelName = vid.Snippet.ChannelTitle, Link = post.Permalink.ToString(), UserName = post.AuthorName } );
                    }
                }

                return toReturn;
            } );
        }
        public UserStalker() {
            InitDatabase();
        }
        public void InitDatabase() {
            using ( DbConnection con = Logging.DirtBagConnection.GetConn() ) {
                string initTables = "" +
                    "CREATE TABLE IF NOT EXISTS [UserPosts]( " +
                    "[PostID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    //"[UserID] INTEGER NOT NULL, " +
                    "[UserName] varchar(50), " +
                    "[Link] varchar(200), " +
                    "[ChannelID] varchar(100), " +
                    "[ChannelName] varchar(200) );" +
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
