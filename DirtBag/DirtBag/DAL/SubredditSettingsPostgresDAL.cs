using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.DAL {
    public class SubredditSettingsPostgresDAL : ISubredditSettingsDAL {
        public async Task<Models.SubredditSettings> GetSubredditSettingsAsync(string subreddit ) {
            using(var conn = Logging.DirtBagConnection.GetSentinelConn() ) {
                return new Models.SubredditSettings(); //TODO
            }
        }
    }
}
