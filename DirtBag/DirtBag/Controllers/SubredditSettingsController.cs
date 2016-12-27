using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DirtBag.Controllers {
    [RoutePrefix( "api/Settings" )]
    public class SubredditSettingsController : ApiController {
        BLL.SubredditSettingsBLL bll;

        public SubredditSettingsController(BLL.SubredditSettingsBLL ssBLL ) {
            bll = ssBLL;
        }

        [Route("{subreddit}"), HttpGet]
        public Task<BotSettings> GetSettings(string subreddit ) {
            return bll.GetSubredditSettingsAsync( subreddit );
        }

        [Route("{subreddit}/Purge"),HttpDelete]
        public void PurgeCacheForSub(string subreddit ) {
            bll.PurgeSubSettingsFromCache( subreddit );
        }
    }
}
