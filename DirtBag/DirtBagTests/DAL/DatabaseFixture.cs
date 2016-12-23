using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using DirtBag.DAL;
using System.Data.SqlClient;

namespace DirtBag.DAL.Tests {
    [TestFixture, Explicit]
    public class DatabaseFixture {
        protected SqlConnection conn;
        [OneTimeSetUp]
        public void DBSetup() {
            //DO NOT CHANGE TO ANYTHING THAT YOU AREN'T WILLING TO WIPE OUT!
            //populate data script truncates tables!!!
            conn = new SqlConnection( "Data Source=(localdb)\\ProjectsV13;Initial Catalog=Dirtbag;Integrated Security=True;Pooling=False;Connect Timeout=30" );
            var initializer = new DatabaseInitializationSQL(conn);
            initializer.InitializeTablesAndData().Wait();

            string insertSub = @"
Truncate table Subreddits;
INSERT INTO Subreddits(SubName)
VALUES('TestSubbie');
";
            conn.Open();
            try {
                var cmd = new SqlCommand( insertSub, conn );
                cmd.ExecuteNonQuery();
            }
            finally {
                conn.Close();
            }
        }

        [SetUp]
        public virtual void TestSetup() {
            RefreshDBData();
        }


        /// <summary>
        /// THIS WILL TRUNCATE DATA! BEWARE!
        /// </summary>
        protected void RefreshDBData() {

            // THIS WILL TRUNCATE DATA! BEWARE!
            string query = @"
truncate table AnalysisScores;
truncate table ProcessedItems;

INSERT INTO ProcessedItems(SubredditID,ThingID,ThingType,ActionID,SeenByModules)
VALUES 
    (1,'t3_test1',1,2,20),
    (1,'t1_test2',2,1,1);

INSERT INTO AnalysisScores(SubredditID, ModuleID, ThingID, Score, Reason, ReportReason, FlairText, FlairClass, FlairPriority)
VALUES
    (1,16,'t3_test1',1.11,'reason1','reportreason1',null,null,null),
    (1,1,'t1_test2',5,'reason2','reportreason2',null,null,null),
    (1,1,'t1_test2',1.5,'reason3','reportreason3','flair1','flairclass1',1),
    (1,1,'t1_test2',2,'reason4','reportreason4','flair2','flairclass2',2)
";
            conn.Open();
            try {
                var cmd = new SqlCommand( query, conn );
                cmd.ExecuteNonQuery();
            }
            finally {
                conn.Close();
            }
        }
    }
}
