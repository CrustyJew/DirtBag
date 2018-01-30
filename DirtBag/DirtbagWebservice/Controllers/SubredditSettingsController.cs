using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace DirtbagWebservice.Controllers {
    [Authorize("DirtbagAdmin")]
    [Route( "api/Settings" )]

    public class SubredditSettingsController : Controller {
        Dirtbag.BLL.ISubredditSettingsBLL bll;

        public SubredditSettingsController( Dirtbag.BLL.ISubredditSettingsBLL ssBLL ) {
            bll = ssBLL;
        }

        [Route("{subreddit}"), HttpGet]
        public Task<Dirtbag.Models.SubredditSettings> GetSettings( [FromRoute]string subreddit, bool defaults = false ) {
            return bll.GetSubredditSettingsAsync( subreddit, true );
        }

        [Route("{subreddit}"), HttpPost]
        public Task UpdateSettings ([FromRoute]string subreddit, [FromQuery] string modifiedBy, [FromBody] Dirtbag.Models.SubredditSettings settings) {
            if ( string.IsNullOrWhiteSpace( modifiedBy ) ) {
                throw new System.Exception("modifiedBy is null");
            }
            if(settings == null) {
                Request.EnableRewind();
                Request.Body.Position = 0;
                string req = new StreamReader(Request.Body).ReadToEnd();

                Response.StatusCode = 400;

                return Task.FromResult(BadRequest(new { message = $"Settings failed to be deserialized or were null. Len:{Request.Body.Length}, {Request.Headers["Content-Length"]} | {req.Length} | {Request.Headers["Content-Type"]}" }));
                
            }
            settings.Subreddit = subreddit;
            return bll.SetSubredditSettingsAsync(settings, modifiedBy);
        }
        [Route("{subreddit}/Purge"),HttpDelete]
        public void PurgeCacheForSub( [FromRoute]string subreddit ) {
            bll.PurgeSubSettingsFromCache( subreddit );
        }
    }
}
