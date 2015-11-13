using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
namespace DirtBag.Logging {
    class PostRemoval {
        public int RemovalID { get; set; }
        public DateTime TimeStamp { get; set; }
        public string ModName { get; set; }
        public string Reason { get; set; }
        public UserPost Post { get; set; }

        public PostRemoval() {

        }
        public PostRemoval(RedditSharp.Things.ModAction action ) {
            TimeStamp = action.TimeStamp.Value;
            ModName = action.ModeratorName;
            Reason = action.Details;
        }

        public static DateTime? GetLastProcessedRemovalDate(string sub ) {
            using ( DbConnection conn = DirtBagConnection.GetConn() ) {
                string query = "" +
                    "select MAX(TimeStamp) " +
                    "FROM PostRemovals pr " +
                    "inner join UserPosts up on pr.PostID = up.PostID " +
                    "WHERE " +
                    "up.Subreddit = @sub ";
                return conn.Query<DateTime?>( query, new { sub } ).Single();
            }
        }

        public static UserPost AddRemoval(PostRemoval removal ) {
            using ( DbConnection conn = DirtBagConnection.GetConn() ) {
                string query = "" +
                    "declare @theUserPost Table( " +
                    "[PostID] INTEGER, " +
                    //"[UserID] INTEGER NOT NULL, " +
                    "[UserName] varchar(50), " +
                    "[Link] varchar(200), " +
                    "[ChannelID] varchar(100), " +
                    "[ChannelName] varchar(200), " +
                    "[Subreddit] varchar(100) ) " +
                    "" +
                    "insert into UserPosts (UserID,Link,ChannelID,ChannelName,Subreddit) " +
                    "select @UserID, @Link, @ChannelID, @ChannelName, @Subreddit " +
                    "WHERE NOT EXISTS " +
                    "(select PostID from UserPosts where Link = @Link) " +
                    "; " +
                    "insert into @theUserPostTable " +
                    "select top 1 PostID, UserName, Link, ChannelID, ChannelName, Subreddit " +
                    "FROM UserPosts " +
                    "Where Link = @Link " +
                    "; " +
                    "insert into PostRemovals (TimeStamp,ModName,Reason,PostID) " +
                    "select @TimeStamp, @ModName, @Reason, PostID " +
                    "from @theUserPost " +
                    "; " +
                    "select * from theUserPost;";

                var toReturn = conn.Query<UserPost>( query, new { removal } ).Single();
                if ( string.IsNullOrEmpty( toReturn.ChannelName ) ) return toReturn;
                else return null;
            }
        }

    }
}
