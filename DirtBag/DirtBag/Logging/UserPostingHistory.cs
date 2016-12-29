using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtBag.Logging {
    public class UserPostingHistory {
        public string UserName { get; set; }
        public Dictionary<string,List<string>> PostingHistory { get; set; }
        public static async Task<UserPostingHistory> GetUserPostingHistory( string userName ) {
            using ( var con = DirtBagConnection.GetConn() ) {
                var query = "" +
                    "Select up.ChannelID, up.ThingID  " +
                    "FROM UserPosts up " +
                    //"left JOIN dirtbag.PostRemovals rem on up.PostID = rem.PostID " +
                    "where username like @UserName " +
                    "and up.ChannelID is not null " +
                    //"group by channelid";
                    "";

                UserPostingHistory results = new UserPostingHistory();
                results.UserName = userName;
                results.PostingHistory = new Dictionary<string, List<string>>();

                var vals = await con.QueryAsync( query, param: new { userName } );
                foreach(dynamic kv in vals ) {
                    if ( !results.PostingHistory.ContainsKey( kv.ChannelID ) )
                        results.PostingHistory.Add( kv.ChannelID, new List<string>() );

                    results.PostingHistory[kv.ChannelID].Add( kv.ThingID );
                }
                return results;
            }
        }

    }
}
