using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DirtBagWebservice.Controllers {
    [Route( "api/Settings" )]
    public class SubredditSettingsController : Controller {
        BLL.SubredditSettingsBLL bll;

        public SubredditSettingsController(BLL.SubredditSettingsBLL ssBLL ) {
            bll = ssBLL;
        }

        [Route("{subreddit}"), HttpGet]
        public Task<Models.SubredditSettings> GetSettings(string subreddit ) {
            return bll.GetSubredditSettingsAsync( subreddit );
        }

        [Route("{subreddit}/Purge"),HttpDelete]
        public void PurgeCacheForSub(string subreddit ) {
            bll.PurgeSubSettingsFromCache( subreddit );
        }
    }
}
