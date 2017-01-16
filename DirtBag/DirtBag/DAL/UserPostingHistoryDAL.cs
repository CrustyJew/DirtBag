using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace DirtBag.DAL {
    public class UserPostingHistoryDAL {
        private IDbConnection _conn;
        public UserPostingHistoryDAL(IDbConnection sentinelConn ) {
            _conn = sentinelConn;
        }

        public async Task<Dictionary<string, string>> GetUserPostingHistoryAsync(string username ) {
            string query = @"
select mi.thing_id as ""key"", media_channel_id as ""value"" from reddit_thing rt
inner join media_info mi on mi.thing_id = rt.thing_id
where author = @username 
AND mi.thing_id like 't3_%'
";
            var results = await _conn.QueryAsync<KeyValuePair<string, string>>( query, new { username } );
            return results.ToDictionary( k => k.Key, v => v.Value );

        }
    }
}
