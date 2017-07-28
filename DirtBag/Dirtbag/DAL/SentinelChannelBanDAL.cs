using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Dirtbag.DAL
{
    public class SentinelChannelBanDAL : ISentinelChannelBanDAL {
        private IDbConnection conn;
        public SentinelChannelBanDAL(IDbConnection dbConn ) {
            conn = dbConn;
        }

        public Task<IEnumerable<Models.SentinelChannelBan>> GetSentinelChannelBan(string sub, string thingid ) {
            //needs distinct currently to handle playlists more gracefully
            string query = @"
SELECT distinct bl.media_channel_id as ""MediaChannelID"", bl.media_author as ""MediaAuthor"", bl.media_platform_id as ""MediaPlatform"", 
    bl.blacklist_utc as ""BlacklistDateUTC"", bl.blacklist_by as ""BlacklistBy"", bl.media_channel_url as ""MediaChannelURL"", case when s.subreddit_name like 'TheSentinelBot' then 'true' else 'false' end as ""GlobalBan""
FROM public.reddit_thing rt 
left join public.media_info mi on mi.thing_id = rt.thing_id
left join public.sentinel_blacklist bl on bl.media_channel_id = mi.media_channel_id 
left join public.subreddit s on s.id = bl.subreddit_id
WHERE (s.subreddit_name like @SubName or s.subreddit_name like 'TheSentinelBot')
AND rt.thing_id = @thingid
";
            return conn.QueryAsync<Models.SentinelChannelBan>(query, new { SubName = sub, thingid});
        }
    }
}
