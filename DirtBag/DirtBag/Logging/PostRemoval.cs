using System;
using System.Linq;
using Dapper;
using RedditSharp.Things;

namespace DirtBag.Logging {
    class PostRemoval {
        public int RemovalID { get; set; }
        private DateTime _timestamp;
        public DateTime TimeStamp {
            get { return _timestamp; }
            set {
                _timestamp = DateTime.SpecifyKind( value, DateTimeKind.Utc );
            }
        }
        public string ModName { get; set; }
        public string Reason { get; set; }
        public UserPost Post { get; set; }

        public PostRemoval() {

        }
        public PostRemoval(ModAction action ) {
            TimeStamp = action.TimeStamp.Value.ToUniversalTime();
            ModName = action.ModeratorName;
            Reason = action.Details;
        }

        public static DateTime? GetLastProcessedRemovalDate(string sub ) {
            using ( var conn = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "select MAX(TimeStamp) " +
                    "FROM PostRemovals pr " +
                    "inner join UserPosts up on pr.PostID = up.PostID " +
                    "WHERE " +
                    "up.Subreddit = @sub ";
                var time = conn.Query<DateTime?>( query, new { sub } ).Single();
                if ( !time.HasValue ) return null;
                if ( DirtBagConnection.UseLocalDB ) return time.Value.ToUniversalTime();
                else return time.Value;
            }
        }

        public static UserPost AddRemoval(PostRemoval removal ) {
            using ( var conn = DirtBagConnection.GetConn() ) {
                var query = "" +
                    //"declare @theUserPost Table( " +
                    //"[PostID] INTEGER, " +
                    ////"[UserID] INTEGER NOT NULL, " +
                    //"[UserName] varchar(50), " +
                    //"[Link] varchar(200), " +
                    //"[ChannelID] varchar(100), " +
                    //"[ChannelName] varchar(200), " +
                    //"[Subreddit] varchar(100) ) " +
                    //"" +
                    "insert into UserPosts (UserName,ThingID,Link,PostTime,ChannelID,ChannelName,Subreddit) " +
                    "select @UserName, @ThingID, @Link, @PostTime, @ChannelID, @ChannelName, @Subreddit " +
                    "WHERE NOT EXISTS " +
                    "(select PostID from UserPosts where Link = @Link) " +
                    "; " +
                    //"insert into @theUserPostTable " +
                    //"select top 1 PostID, UserName, Link, ChannelID, ChannelName, Subreddit " +
                    //"FROM UserPosts " +
                    //"Where Link = @Link " +
                    "; " +
                    "insert into PostRemovals (TimeStamp,ModName,Reason,PostID) " +
                    "select @TimeStamp, @ModName, @Reason, PostID " +
                    "from UserPosts " +
                    "WHERE Link = @Link " + 
                    "; " +
                    "select * from UserPosts where Link = @Link;";

                var toReturn = conn.Query<UserPost>( query, new {
                    removal.Post.UserName,
                    removal.Post.ThingID,
                    removal.Post.Link,
                    removal.Post.PostTime,
                    removal.Post.ChannelID,
                    removal.Post.ChannelName,
                    removal.Post.Subreddit,
                    removal.TimeStamp,
                    removal.ModName,
                    removal.Reason} ).Single();
                if ( string.IsNullOrEmpty( toReturn.ChannelName ) ) return toReturn;
                return null;
            }
        }

    }
}
