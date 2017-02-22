using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DirtBagWebservice.Controllers {
    /* Deprecated for TSB, leaving here for the moment though
     * 
     * 
    [Route( "api/AutomodBanned" )]
    public class AutomodWranglerController : Controller {

        private BotFunctions.AutoModWrangler wrangler;
        private RedditSharp.Reddit client;
        public AutomodWranglerController(IMemoryCache memCache, RedditSharp.Reddit reddit) {
            wrangler = new BotFunctions.AutoModWrangler( memCache );
            client = reddit;
        }
        [HttpGet( "{subname}" )]
        public async Task<IEnumerable<Models.BannedEntity>> Get( string subname ) {
            var sub = await client.GetSubredditAsync( subname );
            wrangler.Subreddit = sub;
            return await wrangler.GetBannedList();
        }

        [HttpPost( "{subname}" )]
        public async Task Post( string subname, IEnumerable<Models.BannedEntity> entities ) {
            var sub = await client.GetSubredditAsync( subname );
            wrangler.Subreddit = sub;
            await wrangler.AddToBanList( entities );
        }

        [HttpDelete( "{subname}" )]
        public async Task Delete( int id, string modName, string subname ) {
            var sub = await client.GetSubredditAsync( subname );
            wrangler.Subreddit = sub;
            await wrangler.RemoveFromBanList( id, modName );
        }

        [HttpPut("{subname}/{id}")]
        public async Task<bool> UpdateBanReason(int id, string modName, [FromBody] string banReason, string subname = "" ) {
            var sub = await client.GetSubredditAsync( subname );
            wrangler.Subreddit = sub;
            return await wrangler.UpdateBanReason( id, subname, modName, banReason );
        }
    }
    */
}
