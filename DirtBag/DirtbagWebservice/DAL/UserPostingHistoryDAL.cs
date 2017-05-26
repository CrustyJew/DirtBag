using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace DirtbagWebservice.DAL {
    public class UserPostingHistoryDAL : IUserPostingHistoryDAL {
        private IDbConnection _conn;
        public UserPostingHistoryDAL(IDbConnection sentinelConn ) {
            _conn = sentinelConn;
        }

        public async Task<IEnumerable<Models.UserPostInfo>> GetUserPostingHistoryAsync(string username ) {
            string query = @"
select mi.thing_id as ""ThingID"", rt.author as ""Username"", mi.media_author as ""MediaAuthor"",
mi.media_channel_id as ""MediaChannelID"", mi.media_platform_id as ""MediaPlatform"", mi.media_url as ""MediaUrl""
from reddit_thing rt
inner join media_info mi on mi.thing_id = rt.thing_id
where rt.author = @username 
AND mi.thing_id like 't3_%'
AND (rt.link_url is not null or rt.link_url <> '')
";
            var results = await _conn.QueryAsync<Models.UserPostInfo>( query, new { username } );
            return results;

        }
    }
}
