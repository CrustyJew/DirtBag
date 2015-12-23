using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace DirtBag.Logging {
    class UserAnalysis {
        //public int UserID { get; set; }
        public string UserName { get; set; }
        public List<UserPost> Posts { get; set; }

        public static UserAnalysis GetUserAnalysis(string UserName ) {
            using(var con = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "Select up.ChannelID,count( up.postID ), count( distinct rem.PostID ) " +
                    "FROM dirtbag.UserPosts up " +
                    "left JOIN dirtbag.PostRemovals rem on up.PostID = rem.PostID " +
                    "where username like @UserName " +
                    "group by channelid"; 

                var analysis = new UserAnalysis();
                analysis.UserName = UserName;
                analysis.Posts = new List<UserPost>();

                var posts = new Dictionary<int, UserPost>();

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
