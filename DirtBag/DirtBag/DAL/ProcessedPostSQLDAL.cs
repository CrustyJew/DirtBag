using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Transactions;

namespace DirtBag.DAL {
    public class ProcessedPostSQLDAL {
        private IDbConnection conn;
        public ProcessedPostSQLDAL( IDbConnection dbConn ) {
            conn = dbConn;
        }

        public async Task LogProcessedItem( Models.ProcessedItem processed ) {
            string processedPostInsert = @"
INSERT INTO ProcessedItems(SubredditID,ThingID,ThingType,ActionID,SeenByModules)
select sub.ID, @ThingID, @ThingType, act.ID, @SeenByModules
from Subreddits sub
inner join Actions act on act.ActionName = @Action
where sub.SubName like @SubName
;";
            string analysisResultsInsert = @"
INSERT INTO AnalysisScores([SubredditID], [ModuleID], [ThingID], [Score], [Reason], [ReportReason], [FlairText], [FlairClass], [FlairPriority])
select sub.ID, @ModuleID, @ThingID, @Score, @Reason, @ReportReason, @FlairText, @FlairClass, @FlairPriority
from Subreddits sub
where sub.SubName like @SubName
;";
            List<Dictionary<string, object>> arParams = new List<Dictionary<string, object>>();
            foreach ( var score in processed.AnalysisDetails.Scores ) {
                arParams.Add( new Dictionary<string, object>() {
                    {"ThingID", processed.ThingID },
                    {"SubName",processed.SubName },
                    {"ModuleID",score.ModuleID },
                    {"Score", score.Score },
                    {"Reason",score.Reason },
                    {"ReportReason",score.ReportReason },
                    {"FlairText",score.RemovalFlair?.Text },
                    {"FlairClass",score.RemovalFlair?.Class },
                    {"FlairPriority",score.RemovalFlair?.Priority }
                } );
            }
            using ( var transactionScope = new TransactionScope( TransactionScopeAsyncFlowOption.Enabled ) ) {
                await conn.ExecuteAsync( processedPostInsert, processed ).ConfigureAwait( false );
                await conn.ExecuteAsync( analysisResultsInsert, arParams ).ConfigureAwait( false );

                transactionScope.Complete();
            }

        }

        public async Task UpdatedAnalysisScores( string thingID, IEnumerable<Modules.AnalysisScore> scores ) {
            string scoresUpdate = @"
DELETE FROM AnalysisScores WHERE ThingID = @thingID AND ModuleID = @ModuleID;

INSERT INTO AnalysisScores([SubredditID], [ModuleID], [ThingID], [Score], [Reason], [ReportReason], [FlairText], [FlairClass], [FlairPriority])
select sub.ID, @ModuleID, @PostID, @Score, @Reason, @ReportReason, @FlairText, @FlairClass, @FlairPriority
from Subreddits sub
where sub.SubName like @SubName
;";
            await conn.ExecuteAsync( scoresUpdate, new { scores, thingID } );
        }

        public async Task<Models.ProcessedItem> ReadProcessedItem( string thingID, string subName ) {
            string query = @"
SELECT subs.SubName, pp.ThingID, pp.ThingType, act.ActionName as 'Action', pp.SeenByModules, 
    scores.Score, scores.Reason, scores.ReportReason, scores.ModuleID, 
    scores.FlairText as 'Text', scores.FlairClass as 'Class', scores.FlairPriority as 'Priority'
FROM ProcessedItems pp
LEFT JOIN AnalysisScores scores on scores.subredditID = pp.subredditID AND pp.thingID = scores.thingID
LEFT JOIN Actions act on act.ID = pp.ActionID
LEFT JOIN Subreddits subs on subs.ID = pp.SubredditID
WHERE
pp.ThingID = @thingID
AND subs.SubName = @subName
";
            Models.ProcessedItem toReturn = null;

            var result = await conn.QueryAsync<Models.ProcessedItem, Models.AnalysisScore, Flair, Models.ProcessedItem>(
                query,
                ( pi, score, flair ) => {
                    if ( toReturn == null ) {
                        toReturn = pi;
                    }

                    score.RemovalFlair = flair;
                    toReturn.AnalysisDetails.Scores.Add( score );

                    return pi;
                },
                splitOn: "Score,Text",
                param: new { thingID, subName } );

            return toReturn;
        }
    }
}
