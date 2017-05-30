using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DirtbagWebservice.Controllers {
    [Authorize("DirtbagAdmin")]
    [Route( "api/Settings" )]

    public class SubredditSettingsController : Controller {
        BLL.ISubredditSettingsBLL bll;

        public SubredditSettingsController(BLL.ISubredditSettingsBLL ssBLL ) {
            bll = ssBLL;
        }

        [Route("{subreddit}"), HttpGet]
        public Task<Models.SubredditSettings> GetSettings( [FromRoute]string subreddit, bool defaults = false ) {
            return bll.GetSubredditSettingsAsync( subreddit, defaults );
        }

        [Route("{subreddit}"), HttpPost]
        public Task UpdateSettings ([FromRoute]string subreddit, [FromQuery] string modifiedBy, [FromBody] Models.SubredditSettings settings) {
            if ( string.IsNullOrWhiteSpace( modifiedBy ) ) {
                throw new System.Exception("modifiedBy is null");
            }
            return bll.SetSubredditSettingsAsync( settings, modifiedBy );
        }
        [Route("{subreddit}/Purge"),HttpDelete]
        public void PurgeCacheForSub( [FromRoute]string subreddit ) {
            bll.PurgeSubSettingsFromCache( subreddit );
        }
    }
}
