using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data.Common;

namespace DirtBag.Logging {
    class UserAnalysis {
        //public int UserID { get; set; }
        public string UserName { get; set; }
        public List<UserPost> Posts { get; set; }

        public static UserAnalysis GetUserAnalysis(string UserName ) {
            using(DbConnection con = DirtBagConnection.GetConn() ) {
                string query = "" +
                    "Select " +
                    //"usr.UserID, usr.UserName, " +
                    "up.UserName, " +
                    "up.PostID, up.UserID, up.Link, " +
                    "up.ChannelID, up.ChannelName, " + 
                    //"chans.ChannelID, chans.Identifier, chans.Name " +
                    "rem.RemovalID,rem.TimeStamp rem.ModName, rem.Reason, rem.PostID " +
                    "FROM Users usr " +
                    "INNER JOIN UserPosts up on usr.UserID = up.UserID " +
                    //"INNER JOIN Channels chans on up.ChannelID = chans.ChannelID " +
                    "INNER JOIN PostRemovals rem on up.PostID = rem.PostID " +                    
                    "WHERE " +
                    "usr.UserName like @UserName";

                UserAnalysis analysis = new UserAnalysis();
                analysis.UserName = UserName;
                analysis.Posts = new List<UserPost>();

                Dictionary<int, UserPost> posts = new Dictionary<int, UserPost>();

                var result = con.Query< UserPost, PostRemoval, UserAnalysis>( query, ( up, pr) => {
                    
                    UserPost post;
                    if (! posts.ContainsKey( up.PostID ) ) {
                        up.Removals = new List<PostRemoval>();
                        posts.Add( up.PostID, up );
                    }
                    post = posts[up.PostID];
                    
                    if(pr != null ) {
                        post.Removals.Add( pr );
                    }

                    return analysis;
                }, splitOn: "RemovalID", param: new { UserName } );

                analysis.Posts = posts.Values.ToList();

                return analysis;
            }
        }
    }
}
