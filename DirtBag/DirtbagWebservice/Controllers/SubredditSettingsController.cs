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
        public Task<Models.SubredditSettings> GetSettings(string subreddit, bool defaults = false ) {
            return bll.GetSubredditSettingsAsync( subreddit, defaults );
        }

        [Route("{subreddit}"), HttpPost]
        public Task UpdateSettings (string subreddit, [FromQuery] string modifiedBy, [FromBody] Models.SubredditSettings settings) {
            if ( string.IsNullOrWhiteSpace( modifiedBy ) ) {
                throw new System.Exception("modifiedBy is null");
            }
            return bll.SetSubredditSettingsAsync( settings, modifiedBy );
        }
        [Route("{subreddit}/Purge"),HttpDelete]
        public void PurgeCacheForSub(string subreddit ) {
            bll.PurgeSubSettingsFromCache( subreddit );
        }
    }
}
