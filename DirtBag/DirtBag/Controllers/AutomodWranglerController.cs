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
            wrangler = new BotFunctions.AutoModWrangler( Task.Run(async () => await Program.Client.GetSubredditAsync( Program.Subreddit )).Result );
        }
        [HttpGet][Route( "{subname?}" )]
        public Task<IEnumerable<Models.BannedEntity>> Get( string subname = "" ) {
            string sub = string.IsNullOrWhiteSpace( subname ) ? Program.Subreddit : subname;
            return wrangler.GetBannedList( );
        }

        [HttpPost][Route( "" )]
        public Task Post( IEnumerable<Models.BannedEntity> entities ) {
            return wrangler.AddToBanList( entities );
        }

        [HttpDelete][Route( "{subname?}" )]
        public Task Delete( int id, string modName, string subname = "" ) {
            string sub = string.IsNullOrWhiteSpace( subname ) ? Program.Subreddit : subname;
            return wrangler.RemoveFromBanList( id, modName );
        }

        [HttpPut][Route("{subname?}/{id}")]
        public Task<bool> UpdateBanReason(int id, string modName, [FromBody] string banReason, string subname = "" ) {
            string sub = string.IsNullOrWhiteSpace( subname ) ? Program.Subreddit : subname;
            return wrangler.UpdateBanReason( id, subname, modName, banReason );
        }
    }
}
