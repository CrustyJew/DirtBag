using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace DirtbagWebservice.DAL {
    public class ProcessedItemSQLDAL : IProcessedItemDAL {
        private IDbConnection conn;
        public ProcessedItemSQLDAL( IDbConnection dbConn ) {
            conn = dbConn;
        }

        public async Task LogProcessedItemAsync( Models.ProcessedItem processed ) {
            string processedPostInsert = @"
INSERT INTO ProcessedItems(SubredditID,ThingID,ThingType,MediaID,MediaPlatform,ActionID,SeenByModules)
select sub.ID, @ThingID, @ThingType,@MediaID, @MediaPlatform, act.ID, @SeenByModules
from Subreddits sub
inner join Actions act on act.ActionName = @Action
where sub.SubName like @SubName
;";
            string analysisResultsInsert = @"
INSERT INTO AnalysisScores([SubredditID], [ModuleID], [ThingID], [MediaID], [MediaPlatform], [Score], [Reason], [ReportReason], [FlairText], [FlairClass], [FlairPriority])
select sub.ID, @ModuleID, @ThingID, @MediaID, @MediaPlatform, @Score, @Reason, @ReportReason, @FlairText, @FlairClass, @FlairPriority
from Subreddits sub
where sub.SubName like @SubName
;";
            List<Dictionary<string, object>> arParams = new List<Dictionary<string, object>>();
            foreach ( var score in processed.AnalysisDetails.Scores ) {
                arParams.Add( new Dictionary<string, object>() {
                    {"ThingID", processed.ThingID },
                    {"MediaID",processed.MediaID },
                    {"MediaPlatform", processed.MediaPlatform },
                    {"SubName",processed.SubName },
                    {"ModuleID",(int) score.Module },
                    {"Score", score.Score },
                    {"Reason",score.Reason },
                    {"ReportReason",score.ReportReason },
                    {"FlairText",score.RemovalFlair?.Text },
                    {"FlairClass",score.RemovalFlair?.Class },
                    {"FlairPriority",score.RemovalFlair?.Priority }
                } );
            }
            using ( var transactionScope = conn.BeginTransaction() ) {
                await conn.ExecuteAsync( processedPostInsert, processed );
                await conn.ExecuteAsync( analysisResultsInsert, arParams );

                transactionScope.Commit();
            }

        }

        public async Task UpdatedAnalysisScoresAsync(string subName, string thingID, string mediaID, Models.VideoProvider mediaPlatform, IEnumerable<Models.AnalysisScore> scores, string updateRequestor ) {
            List<Dictionary<string, object>> asParams = new List<Dictionary<string, object>>();
            foreach ( var score in scores ) {
                asParams.Add( new Dictionary<string, object>() {
                    {"ThingID", thingID },
                    {"SubName",subName },
                    {"MediaID",mediaID },
                    {"MediaPlatform", mediaPlatform },
                    {"ModuleID", (int) score.Module },
                    {"Score", score.Score },
                    {"Reason",score.Reason },
                    {"ReportReason",score.ReportReason },
                    {"FlairText",score.RemovalFlair?.Text },
                    {"FlairClass",score.RemovalFlair?.Class },
                    {"FlairPriority",score.RemovalFlair?.Priority },
                    {"UpdateRequestor", updateRequestor }
                } );
            }
            string scoresDeleteOld = @"
DELETE a
OUTPUT DELETED.ID, GETUTCDATE() as 'HistDate', @UpdateRequestor as 'RequestedBy', DELETED.SubredditID, DELETED.ModuleID, DELETED.ThingID, DELETED.MediaID, DELETED.MediaPlatform, DELETED.Score, DELETED.Reason, DELETED.ReportReason, DELETED.FlairText, DELETED.FlairClass, DELETED.Priority into AnalysisScoresHistory
FROM AnalysisScores a WHERE ThingID = @ThingID AND MediaID = @MediaID AND MediaPlatform = @MediaPlatform AND ModuleID = @ModuleID;
";
            string scoresUpdate = @"
INSERT INTO AnalysisScores([SubredditID], [ModuleID], [ThingID], [MediaID], [MediaPlatform], [Score], [Reason], [ReportReason], [FlairText], [FlairClass], [FlairPriority])
select sub.ID, @ModuleID, @ThingID, @MediaID, @MediaPlatform, @Score, @Reason, @ReportReason, @FlairText, @FlairClass, @FlairPriority
from Subreddits sub
where sub.SubName like @SubName
;";
            using ( var transactionScope = conn.BeginTransaction() ) {
                await conn.ExecuteAsync( scoresDeleteOld, asParams );
                await conn.ExecuteAsync( scoresUpdate, asParams );
                transactionScope.Commit();
            }
        }

        public async Task<Models.ProcessedItem> ReadProcessedItemAsync(string thingID, string subName ) {
            var items = await ReadProcessedItemsAsync( new string[] { thingID }, subName );
            return items.FirstOrDefault();
        }

        public async Task<IEnumerable<Models.ProcessedItem>> ReadProcessedItemsAsync( IEnumerable<string> thingIDs, string subName ) {
            string query = @"
SELECT subs.SubName, pp.ThingID, pp.MediaID, pp.MediaPlatform, pp.ThingType, act.ActionName as 'Action', pp.SeenByModules, 
    scores.Score, scores.Reason, scores.ReportReason, scores.ModuleID, 
    scores.FlairText as 'Text', scores.FlairClass as 'Class', scores.FlairPriority as 'Priority'
FROM ProcessedItems pp
LEFT JOIN AnalysisScores scores on scores.subredditID = pp.subredditID AND pp.thingID = scores.thingID
LEFT JOIN Actions act on act.ID = pp.ActionID
LEFT JOIN Subreddits subs on subs.ID = pp.SubredditID
WHERE
pp.ThingID IN @thingIDs
AND subs.SubName = @subName
";
            Dictionary<string, Models.ProcessedItem> toReturn = new Dictionary<string, Models.ProcessedItem>();

            var result = await conn.QueryAsync<Models.ProcessedItem, Models.AnalysisScore, Models.Flair, Models.ProcessedItem>(
                query,
                ( pi, score, flair ) => {
                    Models.ProcessedItem item;
                    if ( !toReturn.TryGetValue(pi.ThingID, out item) ) {
                        item = pi;
                        toReturn.Add( item.ThingID, item );
                    }
                    score.RemovalFlair = flair;
                    item.AnalysisDetails.Scores.Add( score );

                    return pi;
                },
                splitOn: "Score,Text",
                param: new { thingIDs, subName } );

            return toReturn.Values.AsEnumerable();
        }
    }
}
