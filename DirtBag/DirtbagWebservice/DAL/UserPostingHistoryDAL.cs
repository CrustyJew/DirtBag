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
";
            var results = await _conn.QueryAsync<Models.UserPostInfo>( query, new { username = new CitextParameter(username) } ).ConfigureAwait(false);
            return results;

        }

        public async Task<object> TestUserPostingHistoryAsync (string username ) {
            string query = @"
select mi.thing_id as ""ThingID"", rt.author as ""Username"", mi.media_author as ""MediaAuthor"",
mi.media_channel_id as ""MediaChannelID"", mi.media_platform_id as ""MediaPlatform"", mi.media_url as ""MediaUrl""
from reddit_thing rt
inner join media_info mi on mi.thing_id = rt.thing_id
where rt.author = :u
AND mi.thing_id like 't3_%'
";
            var conn = (Npgsql.NpgsqlConnection) _conn;
            var cmd =  conn.CreateCommand();

            conn.Open();
            cmd.CommandText = String.Format(query,username);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("u", DbType.String));
            //cmd.Prepare();
            cmd.Parameters[0].Value = username;
            var result = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            List<Models.UserPostInfo> toReturn = new List<Models.UserPostInfo>();
            while(result.Read()) {
                toReturn.Add(new Models.UserPostInfo {
                    ThingID = result[0].ToString(),
                    Username = result[1].ToString(),
                    MediaAuthor = result[2].ToString(),
                    MediaChannelID = result[3].ToString(),
                    MediaPlatform = (Models.VideoProvider) Enum.Parse(typeof(Models.VideoProvider), result[4].ToString()),
                    MediaUrl = result[5].ToString()
                });
            }
            conn.Close();
            return toReturn;
        }
    }
}
