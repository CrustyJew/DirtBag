using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DirtBag.Controllers {
    public class RedditConnectionController : ApiController {
        [HttpGet]
        public string BurstStats() {
            var agent = Program.Agent;
            return string.Format( "Last Request: {0}\r\nBurst Start: {1}\r\nRequests this Burst: {2}", agent.LastRequest, agent.BurstStart, agent.RequestsThisBurst );
        }
    }
}
