using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DirtbagWebservice.Controllers {
    [Route("api/Analysis")]
    [Authorize]
    public class PostAnalysisController : Controller {
        private BLL.IAnalyzePostBLL analyzeBLL;
        private BLL.IProcessedItemBLL processedBLL;

        public PostAnalysisController( BLL.IAnalyzePostBLL analyzePostBLL, BLL.IProcessedItemBLL processedPostBLL ) {
            analyzeBLL = analyzePostBLL;
            processedBLL = processedPostBLL;
        }

        [Route("{sub}"), HttpGet]
        public Task<IEnumerable<Models.ProcessedItem>> GetAnalysis( [FromRoute] string sub, [FromQuery] string thingID ) {
            if(!User.HasClaim("uri:snoonotes:subreddit"))
            return processedBLL.ReadProcessedPost(thingID, sub);
        }

        [Route("{sub}"), HttpPut]
        public Task<Models.ProcessedItem> UpdateAnalysis( string sub, [FromQuery]string thingID, [FromQuery]string mediaID, [FromQuery]Models.VideoProvider mediaPlatform ) {
            return analyzeBLL.UpdateAnalysisAsync(sub, thingID, mediaID, mediaPlatform, User.Identity.Name);
        }
    }
}
