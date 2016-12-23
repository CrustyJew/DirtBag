using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.DAL {
    public class ProcessedPostSQLDAL {
        private IDbConnection conn;
        public ProcessedPostSQLDAL( IDbConnection dbConn ) {
            conn = dbConn;
        }

        public Task LogProcessedPost(Models.ProcessedPost processed ) {
            string query = @"
INSERT INTO ProcessedPosts(SubredditID,PostID,ActionID,SeenByModules)
VALUES (
";
        }
    }
}
