using System.Collections.Generic;
using Dapper;
using System;

namespace DirtBag.Logging {
    class UserPost {
        public int PostID { get; set; }
        public string UserName { get; set; }
        public string ThingID { get; set; }
        public string Link { get; set; }
        public DateTime PostTime { get; set; }
        public string ChannelID { get; set; }
        public string ChannelName { get; set; }
        public string Subreddit { get; set; }
        public List<PostRemoval> Removals { get; set; }

        public static void InsertPost( UserPost post ) {
            if ( post.Link.Length > 200 ) post.Link = post.Link.Substring( 0, 200 );
            using ( var conn = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "insert into UserPosts (UserName,ThingID,Link,PostTime,ChannelID,ChannelName,Subreddit) " +
                    "select @UserName, @ThingID, @Link, @PostTime, @ChannelID, @ChannelName, @Subreddit " +
                    "WHERE NOT EXISTS " +
                    "(select PostID from UserPosts where Link = @Link) " +
                    ";";
                conn.Execute( query, new { post.UserName,post.ThingID, post.Link, post.PostTime, post.ChannelID, post.ChannelName, post.Subreddit } );
            }
        }

        public static void UpdatePost( UserPost post ) {
            if ( post.Link.Length > 200 ) post.Link = post.Link.Substring( 0, 200 );
            using ( var conn = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "update UserPosts " +
                    "set UserName = @UserName, ThingID = @ThingID, Link = @Link, PostTime = @PostTime, ChannelID = @ChannelID, " +
                    "ChannelName = @ChannelName, Subreddit = @Subreddit " +
                    "WHERE PostID = @PostID";

                conn.Execute( query, new { post.UserName, post.ThingID, post.Link, post.PostTime, post.ChannelID, post.ChannelName, post.Subreddit, post.PostID } );
            }
        }
    }
}
