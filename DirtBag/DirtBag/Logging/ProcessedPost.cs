﻿using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using Dapper;
using Newtonsoft.Json;
using System.Data;

namespace DirtBag.Logging {
    public class ProcessedPost {
        [JsonProperty]
        public string SubName { get; set; }
        [JsonProperty]
        public string PostID { get; set; }
        [JsonProperty]
        public string Action { get; set; }
        [JsonProperty]
        public Modules.Modules SeenByModules { get; set; }
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
                "insert into ProcessedPosts (SubredditID,PostID,ActionID,SeenByModules,AnalysisResults) " +
                "select sub.ID, @PostID, act.ID, @SeenByModules, @AnalysisResults " +
                "from Subreddits sub " +
                "inner join Actions act on act.ActionName = @Action " +
                "where sub.SubName like @SubName" +
                ";";
            using ( var conn = DirtBagConnection.GetConn() ) {
                conn.Execute( query, new { post.SubName, post.PostID, post.Action,post.SeenByModules, AnalysisResults = serialized } );
            }

        }

        public static void UpdateProcessedPost( ProcessedPost post ) {
            byte[] serialized = Helpers.ProcessedPostHelpers.SerializeAndCompressResults( post );
            var query = "" +
                "Update ProcessedPosts " +
                "Set ActionID = (select ID from Actions where ActionName = @Action), AnalysisResults = @AnalysisResults, SeenByModules = @SeenByModules " +
                //"FROM ProcessedPosts pp " +
                //"inner join Actions act on act.ActionName = @Action " +
                "where PostID like @PostID " +
                ";";
            using ( var conn = DirtBagConnection.GetConn() ) {
                conn.Execute( query, new { AnalysisResults = serialized, post.Action, post.PostID, post.SeenByModules } );
            }

        }

        public static List<ProcessedPost> GetProcessed( List<string> postIDs ) {
            var tableParam = new DataTable();
            tableParam.Columns.Add( "postID", typeof( string ) );
            foreach ( string postID in postIDs ) {
                tableParam.Rows.Add( postID );
            }
            var query = @"
select sub.SubName, p.PostID, act.ActionName as ""Action"",p.SeenByModules, p.AnalysisResults 
from ProcessedPosts p 
inner join Subreddits sub on sub.ID = p.SubredditID 
inner join Actions act on act.ID = p.ActionID 
inner join @postIDs pids on pids.PostID = p.PostID
;";

            using ( var conn = DirtBagConnection.GetConn() ) {
                return conn.Query<ProcessedPost, byte[], ProcessedPost>( query, ( pp, b ) => {
                    pp.AnalysisResults = Helpers.ProcessedPostHelpers.InflateAndDeserializeResults( b );
                    return pp;
                }, splitOn: "AnalysisResults", param: new { postIDs = tableParam.AsTableValuedParameter("postIDs") } ).ToList();
            }
        }
    }
}
