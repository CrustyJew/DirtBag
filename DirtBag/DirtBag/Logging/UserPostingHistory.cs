using Dapper;
using System.Collections.Generic;
using System.Linq;
namespace DirtBag.Logging {
    public class UserPostingHistory {
        public string UserName { get; set; }
        public Dictionary<string,List<string>> PostingHistory { get; set; }
        public static UserPostingHistory GetUserPostingHistory( string userName ) {
            using ( var con = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "Select up.ChannelID, up.postID  " +
                    "FROM dirtbag.UserPosts up " +
                    //"left JOIN dirtbag.PostRemovals rem on up.PostID = rem.PostID " +
                    "where username like @UserName " +
                    //"group by channelid";
                    "";

                UserPostingHistory results = new UserPostingHistory();
                results.UserName = userName;
                results.PostingHistory = new Dictionary<string, List<string>>();

                var vals = con.Query<KeyValuePair<string, string>>( query, param: new { userName } );

                foreach(KeyValuePair<string,string> kv in vals ) {
                    if ( !results.PostingHistory.ContainsKey( kv.Key ) )
                        results.PostingHistory.Add( kv.Key, new List<string>() );

                    results.PostingHistory[kv.Key].Add( kv.Value );
                }
                return results;
            }
        }

    }
}
