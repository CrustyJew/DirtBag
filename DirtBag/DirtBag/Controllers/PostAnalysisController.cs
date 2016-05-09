using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DirtBag.Controllers {
    [RoutePrefix("api/Posts")]
    public class PostAnalysisController : ApiController {
        [HttpGet]
        [Route("Analyze")]
        public async Task<Modules.PostAnalysisResults> AnalyzePost(string id ) {
            var post = new RedditSharp.Reddit( Program.Agent, false ).GetPost( new Uri(id) );
            Modules.PostAnalysisResults results = await Program.AnalyzePost( post );
            return results;
        }
    }
}
