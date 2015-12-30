using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using Dapper;
using Newtonsoft.Json;

namespace DirtBag.Logging {
    public class ProcessedPost {
        public string SubName { get; set; }
        public string PostID { get; set; }
        public string Action { get; set; }
        public Modules.PostAnalysisResults AnalysisResults { get; set; }

        public ProcessedPost() {

        }

        public ProcessedPost( string subName, string postID, string action ) {
            SubName = subName;
            PostID = postID;
            Action = action;
        }

        public void Save() {
            SaveProcessedPost( this );
        }

        public static void SaveProcessedPost( string subName, string postID, string action ) {
            var query = "" +
                "insert into ProcessedPosts (SubredditID,PostID,ActionID) " +
                "select sub.ID, @postID, act.ID " +
                "from Subreddits sub " +
                "inner join Actions act on act.ActionName = @action " +
                "where sub.SubName like @subName" +
                ";"; 
            //using(GZipStream gz = new GZipStream()
            using ( var conn = DirtBagConnection.GetConn() ) {
                conn.Execute( query, new { subName, postID, action } );
            }
        }
        public static void SaveProcessedPost(ProcessedPost post ) {
            string jsonAnalysisResults = JsonConvert.SerializeObject( post.AnalysisResults );
            using ( System.IO.MemoryStream ms = new System.IO.MemoryStream() ) { 
            using(System.IO.Compression.GZipStream gz = new GZipStream(ms, System.IO.Compression.CompressionMode.Compress ) ) {
                byte[] json = System.Text.Encoding.Unicode.GetBytes( jsonAnalysisResults );
                gz.Write( json, 0, json.Length );
            }
               
            }
        }

        public static List<ProcessedPost> CheckProcessed( List<string> postIDs ) {
            var query = "" +
                "select sub.SubName, p.PostID, act.ActionName as \"Action\" " +
                "from ProcessedPosts p " +
                "inner join Subreddits sub on sub.ID = p.SubredditID " +
                "inner join Actions act on act.ID = p.ActionID " +
                "where p.PostID in @postIDs " +
                ";";

            using ( var conn = DirtBagConnection.GetConn() ) {
                return conn.Query<ProcessedPost>( query, new { postIDs } ).ToList();
            }
        }
    }
}
