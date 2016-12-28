using NUnit.Framework;
using DirtBag.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace DirtBag.DAL.Tests {
    [TestFixture()]
    public class ProcessedPostSQLDALTests : DatabaseFixture {
        private ProcessedItemSQLDAL dal;
        [SetUp]
        public void SetupTest() {
            dal = new DAL.ProcessedItemSQLDAL( conn ); 
        }

        [Test()]
        public void LogProcessedItemsTest() {
            var processedPost = new Models.ProcessedItem( "testsubbie", "12345", "Remove", Models.AnalyzableTypes.Post );
            processedPost.SeenByModules = Modules.Modules.HighTechBanHammer | Modules.Modules.UserStalker;
            processedPost.AnalysisDetails.Scores.Add( new Models.AnalysisScore( 1.11, "reason1", "report1", Modules.Modules.HighTechBanHammer ) );
            processedPost.AnalysisDetails.Scores.Add( new Models.AnalysisScore( 2, "reason2", "report2", Modules.Modules.UserStalker, new Flair( "flair5", "flaircss5", 1 ) ) );
            dal.LogProcessedItemAsync( processedPost ).Wait();

            var result = dal.ReadProcessedItemAsync( "12345", "testsubbie" ).Result;
            Assert.NotNull( result );
            Assert.IsTrue( result.Action == "Remove" && result.SeenByModules == (Modules.Modules.HighTechBanHammer | Modules.Modules.UserStalker) && result.ThingType == Models.AnalyzableTypes.Post
                && result.AnalysisDetails.HasFlair && result.AnalysisDetails.FlairClass == "flaircss5" && result.AnalysisDetails.FlairText == "flair5"
                && result.AnalysisDetails.Scores.Count == 2 );
        }

        [Test()]
        public void ReadProcessedItemTest() {
            var result = dal.ReadProcessedItemAsync( "t1_test2", "testsubbie" ).Result;
            Assert.NotNull( result );
            Assert.IsTrue( result.Action == "Report" && result.SeenByModules == Modules.Modules.LicensingSmasher && result.ThingType == Models.AnalyzableTypes.Comment
                && result.AnalysisDetails.HasFlair && result.AnalysisDetails.FlairClass == "flairclass1" && result.AnalysisDetails.FlairText == "flair1 / flair2" 
                && result.AnalysisDetails.Scores.Count == 3 && result.AnalysisDetails.TotalScore == 8.5 );
        }

        [Test()]
        public void UpdateScoresTest() {
            dal.UpdatedAnalysisScoresAsync( "t1_test2", "testsubbie", new Models.AnalysisScore[] { new Models.AnalysisScore( 5.5, "updatedreason", "updatedreport", Modules.Modules.LicensingSmasher ) } ).Wait();
            var result = dal.ReadProcessedItemAsync( "t1_test2", "testsubbie" ).Result;
            Assert.NotNull( result );
            Assert.That( result.AnalysisDetails.Scores, Has.Count.EqualTo(1) );
            Assert.That( result.AnalysisDetails, Has.Property( "TotalScore" ).EqualTo( 5.5 ) );

            string query = @"
SELECT count(*) from analysisscores
";
            conn.Open();
            int rows = -1;
            try {
                var cmd = new SqlCommand( query, conn );
                rows = (int) cmd.ExecuteScalar();
            }
            finally {
                conn.Close();
            }
            Assert.That( rows, Is.EqualTo(2) );
        }
    }
}