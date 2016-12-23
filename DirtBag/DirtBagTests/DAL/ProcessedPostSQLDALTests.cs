using NUnit.Framework;
using DirtBag.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.DAL.Tests {
    [TestFixture()]
    public class ProcessedPostSQLDALTests : DatabaseFixture {
        private ProcessedPostSQLDAL dal;
        [SetUp]
        public void SetupTest() {
            dal = new DAL.ProcessedPostSQLDAL( conn ); 
        }

        [Test()]
        public void LogProcessedItemsTest() {
            var processedPost = new Models.ProcessedItem( "testsubbie", "12345", "Remove", Models.AnalyzableTypes.Post );
            processedPost.SeenByModules = Modules.Modules.HighTechBanHammer | Modules.Modules.UserStalker;
            processedPost.AnalysisDetails.Scores.Add( new Models.AnalysisScore( 1.11, "reason1", "report1", "module1", 1 ) );
            processedPost.AnalysisDetails.Scores.Add( new Models.AnalysisScore( 2, "reason2", "report2", "module2", 2, new Flair( "flair5", "flaircss5", 1 ) ) );
            dal.LogProcessedItem( processedPost ).Wait();

            var result = dal.ReadProcessedItem( "12345", "testsubbie" ).Result;
            Assert.NotNull( result );
            Assert.IsTrue( result.Action == "Remove" && result.SeenByModules == (Modules.Modules.HighTechBanHammer | Modules.Modules.UserStalker) && result.ThingType == Models.AnalyzableTypes.Post
                && result.AnalysisDetails.HasFlair && result.AnalysisDetails.FlairClass == "flaircss5" && result.AnalysisDetails.FlairText == "flair5"
                && result.AnalysisDetails.Scores.Count == 2 );
        }

        [Test()]
        public void ReadProcessedItemTest() {
            var result = dal.ReadProcessedItem( "t1_test2", "testsubbie" ).Result;
            Assert.NotNull( result );
            Assert.IsTrue( result.Action == "Report" && result.SeenByModules == Modules.Modules.LicensingSmasher && result.ThingType == Models.AnalyzableTypes.Comment
                && result.AnalysisDetails.HasFlair && result.AnalysisDetails.FlairClass == "flairclass1" && result.AnalysisDetails.FlairText == "flair1 / flair2" 
                && result.AnalysisDetails.Scores.Count == 3 && result.AnalysisDetails.TotalScore == 8.5 );
        }
    }
}