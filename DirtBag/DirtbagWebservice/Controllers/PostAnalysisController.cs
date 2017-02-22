using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DirtBagWebservice.Controllers {
    [Route("api/Analysis")]
    public class PostAnalysisController : Controller
    {
        private BLL.AnalyzePostBLL analyzeBLL;

        public PostAnalysisController(BLL.AnalyzePostBLL analyzePostBLL)
        {
            analyzeBLL = analyzePostBLL;
        }

        [HttpPost]
        [Route("{sub}")]
        public async Task<Models.AnalysisResults> AnalyzeDemo(string sub, Models.AnalysisRequest req)
        {
            var analysis = new Models.AnalysisResults();
            Models.AnalysisDetails results = new Models.AnalysisDetails();
            string[] reasons = { "Some module no likey", "spam spam spam", "HEY, LISTEN!", "Giggity giggity", "Totally not a canned response", "OOPS" };
            Modules.Modules[] modules = new Modules.Modules[] { Modules.Modules.HighTechBanHammer, Modules.Modules.LicensingSmasher, Modules.Modules.SelfPromotionCombustor, Modules.Modules.YouTubeSpamDetector };
            Random rngJesus = new Random((int)DateTime.UtcNow.Ticks);
            for (int i = 0; i < rngJesus.Next(0, 10); i++)
            {
                var score = new Models.AnalysisScore(Math.Round(rngJesus.NextDouble() * 10, 2), reasons[rngJesus.Next(0, reasons.Length)], reasons[rngJesus.Next(0, reasons.Length)], modules[rngJesus.Next(0, modules.Length)]);
                if (rngJesus.Next(0, 10) > 6)
                {
                    score.RemovalFlair = new Models.Flair("FlairText" + i, "FlairCSS" + i, rngJesus.Next(1, 5));
                }
                results.Scores.Add(score);
            }
            analysis.AnalysisDetails = results;
            if (results.TotalScore > 12)
            {
                analysis.RequiredAction = Models.AnalysisResults.Action.Remove;
            }
            else if (results.TotalScore > 8)
            {
                analysis.RequiredAction = Models.AnalysisResults.Action.Report;
            }
            else
            {
                analysis.RequiredAction = Models.AnalysisResults.Action.Nothing;
            }
            return analysis;
        }

        [Route("Echo"), HttpPost]
        public Models.AnalysisRequest Echo(Models.AnalysisRequest req)
        {
            return req;
        }
        
        [Route("test/{sub}"), HttpPost]
        public Task<Models.AnalysisResults> Analyze(string sub, Models.AnalysisRequest req)
        {
            return analyzeBLL.AnalyzePost(sub,req);
        }
    }
}
