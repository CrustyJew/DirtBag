using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.DAL {
    internal class SubredditSettingsDAL {
        internal async Task<BotSettings> GetSubredditSettingsAsync(string subreddit ) {
            using(var conn = Logging.DirtBagConnection.GetSentinelConn() ) {
                return new BotSettings(); //TODO
            }
        }
    }
}
