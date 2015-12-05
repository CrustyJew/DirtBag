using System.Collections.Generic;
using Dapper;

namespace DirtBag.Logging {
    class UserPost {
        public int PostID { get; set; }
        public string UserName { get; set; }
        public string Link { get; set; }
        public string ChannelID { get; set; }
        public string ChannelName { get; set; }
        public string Subreddit { get; set; }
        public List<PostRemoval> Removals { get; set; }

        public static void InsertPost( UserPost post ) {
            using ( var conn = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "insert into UserPosts (UserName,Link,ChannelID,ChannelName,Subreddit) " +
                    "select @UserName, @Link, @ChannelID, @ChannelName, @Subreddit " +
                    "WHERE NOT EXISTS " +
                    "(select PostID from UserPosts where Link = @Link) " +
                    ";";
                conn.Execute( query, new { post.UserName, post.Link, post.ChannelID, post.ChannelName, post.Subreddit } );
            }
        }

        public static void UpdatePost( UserPost post ) {
            using ( var conn = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "update UserPosts " +
                    "set UserName = @UserName, Link = @Link, ChannelID = @ChannelID, " +
                    "ChannelName = @ChannelName, Subreddit = @Subreddit " +
                    "WHERE PostID = @PostID";

                conn.Execute( query, new { post.UserName, post.Link, post.ChannelID, post.ChannelName, post.Subreddit, post.PostID } );
            }
        }
    }
}
