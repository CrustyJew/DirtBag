using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DirtBag.Controllers {
    [RoutePrefix( "api/AutomodBanned" )]
    public class AutomodWranglerController : ApiController {

        private BotFunctions.AutoModWrangler wrangler;
        public AutomodWranglerController() {
            wrangler = new BotFunctions.AutoModWrangler( Program.Client.GetSubreddit( Program.Subreddit ) );
        }
        [HttpGet]
        [Route( "{subname?}" )]
        public Task<IEnumerable<Models.BannedEntity>> Get( string subname = "" ) {
            string sub = string.IsNullOrWhiteSpace( subname ) ? Program.Subreddit : subname;
            return wrangler.GetBannedList( sub );
        }

        [HttpPost]
        [Route( "" )]
        public Task Post( IEnumerable<Models.BannedEntity> entities ) {
            return wrangler.AddToBanList( entities );
        }

        [HttpDelete]
        [Route( "{subname?}" )]
        public Task Delete( int id, string modName, string subname = "" ) {
            string sub = string.IsNullOrWhiteSpace( subname ) ? Program.Subreddit : subname;
            return wrangler.RemoveFromBanList( id, sub, modName );
        }

    }
}
