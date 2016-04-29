using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DirtBag.Controllers {
    [RoutePrefix("api/Info")]
    public class InfoController : ApiController {
        [HttpGet][Route("TestConnection")]
        public bool TestConnection(string subreddit ) {
            if(Program.Subreddit.ToLower() == subreddit.ToLower() ) {
                return true;
            }
            else { throw new HttpResponseException( System.Net.HttpStatusCode.NotImplemented ); }
        }
    }
}
