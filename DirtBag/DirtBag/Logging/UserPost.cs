using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
namespace DirtBag.Logging {
    class UserPost {
        public int PostID { get; set; }
        public string UserName { get; set; }
        public string Link { get; set; }
        public string ChannelID { get; set; }
        public string ChannelName { get; set; }
        public List<PostRemoval> Removals { get; set; }

        public static void InsertPost(UserPost post ) {
            using (DbConnection con = DirtBagConnection.GetConn() ) {
                string query = "" +
                    "insert into UserPosts (UserID,Link,ChannelID,ChannelName) " +
                    "select @UserID, @Link, @ChannelID, @ChannelName " +
                    "WHERE NOT EXISTS " +
                    "(select PostID from UserPosts where Link = @Link) " +
                    ";";
                con.Execute( query, new { post.UserName, post.Link, post.ChannelID, post.ChannelName } );
            }
        }
    }
}
