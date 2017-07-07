using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DirtbagWebservice.Controllers {
    [Route("api/Analysis")]
    [Authorize]
    public class PostAnalysisController : Controller {
        private BLL.IAnalyzeMediaBLL analyzeBLL;
        private BLL.IProcessedItemBLL processedBLL;

        public PostAnalysisController( BLL.IAnalyzeMediaBLL analyzePostBLL, BLL.IProcessedItemBLL processedPostBLL ) {
            analyzeBLL = analyzePostBLL;
            processedBLL = processedPostBLL;
        }

        [HttpGet("{sub}")]
        public Task<Models.AnalysisResponse> GetAnalysis( [FromRoute] string sub, [FromQuery] string thingID ) {
            if(!User.IsInRole(sub.ToLower())) throw new UnauthorizedAccessException("Not a mod of that sub!");
            return processedBLL.ReadThingAnalysis(thingID, sub);
        }

        //[HttpPost("{sub}")]
        //public Task<Models.AnalysisResults> DoAnalysis( [FromRoute] string sub, [FromBody] Models.AnalysisRequest request ) {
        //    if(!User.IsInRole(sub.ToLower())) throw new UnauthorizedAccessException("Not a mod of that sub!");
        //    return analyzeBLL.AnalyzeMedia(sub, request, false);
        //}

        [HttpPut("{sub}")]
        public Task<Models.AnalysisResponse> UpdateAnalysis( string sub, [FromQuery]string thingID, [FromQuery]string mediaID, [FromQuery]Models.VideoProvider mediaPlatform ) {
            if(!User.IsInRole(sub.ToLower())) throw new UnauthorizedAccessException("Not a mod of that sub!");
            return analyzeBLL.UpdateAnalysisAsync(sub, thingID, User.Identity.Name);
        }

    }
}
