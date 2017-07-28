using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dirtbag.Models;

namespace Dirtbag.DAL {
    public class ProcessedItemSQLDAL : IProcessedItemDAL {
        private IDbConnection conn;
        public ProcessedItemSQLDAL( IDbConnection dbConn ) {
            conn = dbConn;
        }

        public async Task LogProcessedItemAsync( Models.ProcessedItem processed ) {
            string processedPostInsert = @"

MERGE Subreddits WITH (HOLDLOCK) AS s 
Using (VALUES (@SubName)) AS n (SubName) 
ON s.SubName = n.SubName 
WHEN NOT MATCHED BY TARGET THEN 
INSERT (SubName) VALUES (n.SubName);


INSERT INTO ProcessedItems(SubredditID,ThingID,Author,PermaLink,ThingType,MediaID,MediaChannelID,MediaChannelName,MediaPlatform,ActionID,SeenByModules)
select sub.ID, @ThingID, @Author, @PermaLink, @ThingType, @MediaID, @MediaChannelID, @MediaChannelName, @MediaPlatform, act.ID, @SeenByModules
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
            foreach(var score in processed.AnalysisDetails.Scores) {
                arParams.Add(new Dictionary<string, object>() {
                    {"ThingID", processed.ThingID },
                    {"MediaID",processed.MediaID },
                    {"MediaChannelID",processed.MediaChannelID },
                    {"MediaPlatform", processed.MediaPlatform },
                    {"SubName",processed.SubName },
                    {"Author", processed.Author },
                    {"ModuleID",(int) score.Module },
                    {"Score", score.Score },
                    {"Reason",score.Reason },
                    {"ReportReason",score.ReportReason },
                    {"FlairText",score.RemovalFlair?.Text },
                    {"FlairClass",score.RemovalFlair?.Class },
                    {"FlairPriority",score.RemovalFlair?.Priority }
                });
            }
            if(conn.State != ConnectionState.Open) conn.Open();
            using(var transactionScope = conn.BeginTransaction()) {
                await conn.ExecuteAsync(processedPostInsert, processed, transactionScope);
                await conn.ExecuteAsync(analysisResultsInsert, arParams, transactionScope);

                transactionScope.Commit();
            }

        }

        public async Task UpdatedAnalysisScoresAsync( string subName, string thingID, string mediaID, Models.VideoProvider mediaPlatform, IEnumerable<Models.AnalysisScore> scores, string updateRequestor ) {
            List<Dictionary<string, object>> asParams = new List<Dictionary<string, object>>();
            foreach(var score in scores) {
                asParams.Add(new Dictionary<string, object>() {
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
                });
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
            if(conn.State != ConnectionState.Open) conn.Open();
            using(var transactionScope = conn.BeginTransaction()) {
                await conn.ExecuteAsync(scoresDeleteOld, asParams, transactionScope);
                await conn.ExecuteAsync(scoresUpdate, asParams, transactionScope);
                transactionScope.Commit();
            }
        }

        public async Task<Models.ProcessedItem> ReadProcessedItemAsync( string thingID, string subName) {
            return (await ReadProcessedItemsAsync(new string[] { thingID }, subName).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<IEnumerable<Models.ProcessedItem>> ReadProcessedItemsAsync( IEnumerable<string> thingIDs, string subName ) {
            string query = @"
SELECT subs.SubName, pp.ThingID, pp.Author, pp.MediaID, pp.MediaChannelID, pp.MediaPlatform, pp.ThingType, act.ActionName as 'Action', pp.SeenByModules, 
    scores.Score, scores.Reason, scores.ReportReason, scores.ModuleID, 
    scores.FlairText as 'Text', scores.FlairClass as 'Class', scores.FlairPriority as 'Priority'
FROM ProcessedItems pp
LEFT JOIN AnalysisScores scores on scores.subredditID = pp.subredditID AND pp.thingID = scores.thingID AND pp.mediaID = scores.mediaID AND pp.MediaPlatform = scores.MediaPlatform
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
                    if(!toReturn.TryGetValue(pi.ThingID, out item)) {
                        item = pi;
                        toReturn.Add(item.ThingID, item);
                    }
                    score.RemovalFlair = flair;
                    item.AnalysisDetails.Scores.Add(score);

                    return pi;
                },
                splitOn: "Score,Text",
                param: new { thingIDs, subName }).ConfigureAwait(false);

            return toReturn.Values?.AsEnumerable();
        }

        public async Task<AnalysisResponse> GetThingAnalysis( string thingID, string subName ) {
            string query = @"
SELECT subs.SubName, pp.ThingID, pp.Author, pp.ThingType, act.ActionName as 'Action', pp.SeenByModules, pp.PermaLink, 
    pp.MediaID, pp.MediaChannelID, pp.MediaChannelName, pp.MediaPlatform,
    scores.Score, scores.Reason, scores.ReportReason, scores.ModuleID as 'Module', 
    scores.FlairText as 'Text', scores.FlairClass as 'Class', scores.FlairPriority as 'Priority'
FROM ProcessedItems pp
LEFT JOIN AnalysisScores scores on scores.subredditID = pp.subredditID AND pp.thingID = scores.thingID AND pp.mediaID = scores.mediaID AND pp.MediaPlatform = scores.MediaPlatform
LEFT JOIN Actions act on act.ID = pp.ActionID
LEFT JOIN Subreddits subs on subs.ID = pp.SubredditID
WHERE
pp.ThingID = @thingID
AND subs.SubName = @subName
";
            AnalysisResponse toReturn = null;
            await conn.QueryAsync<AnalysisResponse,MediaAnalysis, AnalysisScore, Flair, AnalysisResponse>(query, (ar,media,score,flair)=> {
                
                if(toReturn == null) {
                    toReturn = ar;
                }
                var mediaAnalysis = toReturn.Analysis.SingleOrDefault(a => a.MediaChannelID == media.MediaChannelID && a.MediaID == media.MediaID && a.MediaPlatform == media.MediaPlatform);
                if(mediaAnalysis == null) {
                    mediaAnalysis = media;
                    toReturn.Analysis.Add(mediaAnalysis);
                }
                if(score != null) {
                    //only 1 flair attached to a score so don't have to do list bullshit
                    score.RemovalFlair = flair ?? new Flair();
                    mediaAnalysis.Scores.Add(score);
                }
                return ar;
            },splitOn:"MediaID,Score,Text",param: new { thingID, subName }).ConfigureAwait(false);

            return toReturn;
        }

        public async Task<IEnumerable<AnalysisResponse>> GetThingsAnalysis( IEnumerable<string> thingIDs, string subName ) {
            string query = @"
SELECT subs.SubName, pp.ThingID, pp.Author, pp.ThingType, act.ActionName as 'Action', pp.SeenByModules, pp.PermaLink, 
    pp.MediaID, pp.MediaChannelID, pp.MediaChannelName, pp.MediaPlatform,
    scores.Score, scores.Reason, scores.ReportReason, scores.ModuleID as 'Module', 
    scores.FlairText as 'Text', scores.FlairClass as 'Class', scores.FlairPriority as 'Priority'
FROM ProcessedItems pp
LEFT JOIN AnalysisScores scores on scores.subredditID = pp.subredditID AND pp.thingID = scores.thingID AND pp.mediaID = scores.mediaID AND pp.MediaPlatform = scores.MediaPlatform
LEFT JOIN Actions act on act.ID = pp.ActionID
LEFT JOIN Subreddits subs on subs.ID = pp.SubredditID
WHERE
pp.ThingID in @thingIDs
";
            List<AnalysisResponse> toReturn = new List<AnalysisResponse>();
            await conn.QueryAsync<AnalysisResponse, MediaAnalysis, AnalysisScore, Flair, AnalysisResponse>(query, ( ar, media, score, flair ) => {
                if(ar.SubName.ToLower() != subName.ToLower()) return ar;

                var current = toReturn.SingleOrDefault(r => r.ThingID == ar.ThingID);
                if(current == null) {
                    current = ar;
                    toReturn.Add(current);
                }
                var mediaAnalysis = current.Analysis.SingleOrDefault(a => a.MediaChannelID == media.MediaChannelID && a.MediaID == media.MediaID && a.MediaPlatform == media.MediaPlatform);
                if(mediaAnalysis == null) {
                    mediaAnalysis = media;
                    current.Analysis.Add(mediaAnalysis);
                }
                if(score != null) {
                    //only 1 flair attached to a score so don't have to do list bullshit
                    score.RemovalFlair = flair ?? new Flair();
                    mediaAnalysis.Scores.Add(score);
                }
                return ar;
            }, splitOn: "MediaID,Score,Text", param: thingIDs).ConfigureAwait(false);

            return toReturn;
        }
    }
}
