using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DirtBag.Controllers {
    [RoutePrefix( "api/Analysis" )]
    public class PostAnalysisController : ApiController {
        [HttpPost]
        [Route( "{sub}" )]
        public async Task<Models.AnalysisResults> Analyze( string sub, Models.AnalysisRequest req ) {
            var analysis = new Models.AnalysisResults();
            Modules.PostAnalysisResults results = new Modules.PostAnalysisResults();
            string[] reasons = { "Some module no likey", "spam spam spam", "HEY, LISTEN!", "Giggity giggity", "Totally not a canned response", "OOPS" };
            string[] modules = { "GrrSmash", "StronkSpam", "JukinBribeSystem", "SpezDBEditor" };
            Random rngJesus = new Random( (int) DateTime.UtcNow.Ticks );
            for ( int i = 0; i < rngJesus.Next( 0, 10 ); i++ ) {
                var score = new Modules.AnalysisScore( Math.Round( rngJesus.NextDouble() * 10, 2 ), reasons[rngJesus.Next( 0, reasons.Length )], reasons[rngJesus.Next( 0, reasons.Length )], modules[rngJesus.Next( 0, modules.Length )] );
                if ( rngJesus.Next( 0, 10 ) > 6 ) {
                    score.RemovalFlair = new DirtBag.Flair( "FlairText" + i, "FlairCSS" + i, rngJesus.Next( 1, 5 ) );
                }
                results.Scores.Add( score );
            }
            analysis.AnalysisDetails = results;
            if ( results.TotalScore > 12) {
                analysis.RequiredAction = Models.AnalysisResults.Action.Remove;
            }
            else if ( results.TotalScore > 8 ) {
                analysis.RequiredAction = Models.AnalysisResults.Action.Report;
            }
            else {
                analysis.RequiredAction = Models.AnalysisResults.Action.Nothing;
            }
            return analysis;
        }

        [Route( "Echo" ), HttpPost]
        public Models.AnalysisRequest Echo( Models.AnalysisRequest req ) {
            return req;
        }


    }
}
