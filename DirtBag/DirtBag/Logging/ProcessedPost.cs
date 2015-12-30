using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using Dapper;
using Newtonsoft.Json;

namespace DirtBag.Logging {
    public class ProcessedPost {
        [JsonProperty]
        public string SubName { get; set; }
        [JsonProperty]
        public string PostID { get; set; }
        [JsonProperty]
        public string Action { get; set; }
        [JsonProperty]
        public Modules.PostAnalysisResults AnalysisResults { get; set; }

        public ProcessedPost() {

        }

        public ProcessedPost( string subName, string postID, string action ) {
            SubName = subName;
            PostID = postID;
            Action = action;
            AnalysisResults = new Modules.PostAnalysisResults();
        }

        public void Save() {
            AddProcessedPost( this );
        }

        public static void AddProcessedPost( ProcessedPost post ) {
            //string jsonAnalysisResults = JsonConvert.SerializeObject( post.AnalysisResults,Formatting.None );
            byte[] serialized = Helpers.ProcessedPostHelpers.SerializeAndCompressResults( post );//System.Text.Encoding.ASCII.GetBytes( jsonAnalysisResults ); //This would need to change to support unicode report reasons & full explanations
            var query = "" +
                "insert into ProcessedPosts (SubredditID,PostID,ActionID,AnalysisResults) " +
                "select sub.ID, @PostID, act.ID, @AnalysisResults " +
                "from Subreddits sub " +
                "inner join Actions act on act.ActionName = @Action " +
                "where sub.SubName like @SubName" +
                ";";
            using ( var conn = DirtBagConnection.GetConn() ) {
                conn.Execute( query, new { post.SubName, post.PostID, post.Action, AnalysisResults = serialized } );
            }

        }

        public static void UpdateProcessedPost( ProcessedPost post ) {
            byte[] serialized = Helpers.ProcessedPostHelpers.SerializeAndCompressResults( post );
            var query = "" +
                "Update pp " +
                "Set pp.ActionID = act.ID, pp.AnalysisResults = @AnalysisResults " +
                "FROM ProcessedPosts pp " +
                "inner join Actions act on act.ActionName = @Action " +
                "where pp.PostID = @PostID " +
                ";";
            using ( var conn = DirtBagConnection.GetConn() ) {
                conn.Execute( query, new { AnalysisResults = serialized, post.Action, post.PostID } );
            }

        }

        public static List<ProcessedPost> CheckProcessed( List<string> postIDs ) {
            var query = "" +
                "select sub.SubName, p.PostID, act.ActionName as \"Action\", p.AnalysisResults " +
                "from ProcessedPosts p " +
                "inner join Subreddits sub on sub.ID = p.SubredditID " +
                "inner join Actions act on act.ID = p.ActionID " +
                "where p.PostID in @postIDs " +
                ";";

            using ( var conn = DirtBagConnection.GetConn() ) {
                return conn.Query<ProcessedPost, byte[], ProcessedPost>( query, ( pp, b ) => {
                    pp.AnalysisResults = Helpers.ProcessedPostHelpers.InflateAndDeserializeResults( b );
                    return pp;
                }, splitOn: "AnalysisResults", param: new { postIDs } ).ToList();
            }
        }
    }
}
