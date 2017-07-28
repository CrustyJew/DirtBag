using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dirtbag.BLL {
    public class AnalyzeMediaBLL : IAnalyzeMediaBLL {
        private BLL.ISubredditSettingsBLL subSetsBLL;
        private DAL.IUserPostingHistoryDAL postHistoryDAL;
        private IConfigurationRoot config;
        private DAL.IProcessedItemDAL processedDAL;
        private RedditSharp.WebAgentPool<string, RedditSharp.BotWebAgent> botAgentPool;

        private ILogger<AnalyzeMediaBLL> logger;
        public AnalyzeMediaBLL(IConfigurationRoot config, BLL.ISubredditSettingsBLL settingsBLL, DAL.IUserPostingHistoryDAL userPostHistoryDAL, DAL.IProcessedItemDAL processedItemDAL, RedditSharp.WebAgentPool<string, RedditSharp.BotWebAgent> botAgentPool, ILogger<AnalyzeMediaBLL> logger )
        {
            subSetsBLL = settingsBLL;
            postHistoryDAL = userPostHistoryDAL;
            this.config = config;
            processedDAL = processedItemDAL;
            this.botAgentPool = botAgentPool;
            this.logger = logger;
        }

        public async Task UpdateAnalysisAsync(IEnumerable<string> thingIDs, string subreddit, string updateBy ) {
            var previousResults = await processedDAL.GetThingsAnalysis(thingIDs, subreddit).ConfigureAwait(false);

            List<Models.AnalysisRequest> analysisRequests = new List<Models.AnalysisRequest>();

            foreach(var prev in previousResults) {
                foreach(var analysis in prev.Analysis) {
                    Models.AnalysisRequest request = new Models.AnalysisRequest() {
                        MediaChannelID = analysis.MediaChannelID,
                        MediaID = analysis.MediaID,
                        MediaPlatform = analysis.MediaPlatform,
                        ThingID = prev.ThingID,
                        PermaLink = prev.PermaLink
                    };

                    analysisRequests.Add(request);
                }
            }
            
            var settings = await subSetsBLL.GetSubredditSettingsAsync(subreddit);



                List<Task<Dictionary<string, Models.AnalysisDetails>>> analysisTasks = new List<Task<Dictionary<string, Models.AnalysisDetails>>>();

                    //Only rerun things that don't need to run at the time a post was made
                    if(settings.LicensingSmasher.Enabled) {
                        analysisTasks.Add(new Modules.LicensingSmasher(config, settings.LicensingSmasher, subreddit).Analyze(analysisRequests));
                    }

                    if(analysisTasks.Count == 0) {
                        //module disabled, bail out, don't update
                        return;
                    }

                    var updatedResults = await CombineResults(analysisTasks, settings).ConfigureAwait(false);
            foreach(var updResult in updatedResults) {
                foreach(var updMediaIDResult in updResult.Value) {
                    await processedDAL.UpdatedAnalysisScoresAsync(subreddit, updResult.Key, updMediaIDResult.AnalysisDetails.MediaID, updMediaIDResult.AnalysisDetails.MediaPlatform, updMediaIDResult.AnalysisDetails.Scores, updateBy).ConfigureAwait(false);

                    var newResults = await processedDAL.GetThingAnalysis(updResult.Key, subreddit).ConfigureAwait(false);
                    Models.AnalysisResults.Action newAction = Models.AnalysisResults.Action.None;

                    if(newResults.HighScore >= settings.RemoveScoreThreshold && settings.RemoveScoreThreshold > 0) {
                        newAction = Models.AnalysisResults.Action.Remove;
                    }
                    else if(newResults.HighScore >= settings.ReportScoreThreshold && settings.ReportScoreThreshold > 0) {
                        newAction = Models.AnalysisResults.Action.Report;
                    }
                    else {
                        newAction = Models.AnalysisResults.Action.None;
                    }

                    if(botAgentPool != null && newAction != Models.AnalysisResults.Action.None && (string.IsNullOrWhiteSpace(config["SkipBotActions"]) || config["SkipBotActions"].ToLower() == "false")) {
                        var agent = await botAgentPool.GetOrCreateAgentAsync(settings.BotName, () => {
                            logger.LogInformation("Creating web agent for " + settings.BotName);
                            var toReturn = new RedditSharp.BotWebAgent(settings.BotName, settings.BotPass, settings.BotAppID, settings.BotAppSecret, null);
                            toReturn.RateLimiter = new RedditSharp.RateLimitManager(RedditSharp.RateLimitMode.SmallBurst);
                            return Task.FromResult(toReturn);
                        });

                        var prevResults = previousResults.SingleOrDefault(r => r.ThingID == updResult.Key);

                        if(newAction == Models.AnalysisResults.Action.Remove && prevResults?.Action != "Remove") {
                            logger.LogInformation($"Removing thing {updMediaIDResult.AnalysisDetails.ThingID} - {prevResults.PermaLink}");
                            await RedditSharp.Things.ModeratableThing.RemoveAsync(agent, updMediaIDResult.AnalysisDetails.ThingID).ConfigureAwait(false);
                            if(updMediaIDResult.AnalysisDetails.ThingType == Models.AnalyzableTypes.Post && updMediaIDResult.AnalysisDetails.HasFlair) {
                                await RedditSharp.Things.Post.SetFlairAsync(agent, settings.Subreddit, updMediaIDResult.AnalysisDetails.ThingID, updMediaIDResult.AnalysisDetails.FlairText, updMediaIDResult.AnalysisDetails.FlairClass).ConfigureAwait(false);
                                await processedDAL.UpdateProcessedPostActionAsync(subreddit, updMediaIDResult.AnalysisDetails.ThingID, updMediaIDResult.AnalysisDetails.MediaID, updMediaIDResult.AnalysisDetails.MediaPlatform, "Remove").ConfigureAwait(false);
                            }
                        }
                        else if(newAction == Models.AnalysisResults.Action.Report && prevResults.Action != "Report") {

                            logger.LogInformation($"Reporting thing {updMediaIDResult.AnalysisDetails.ThingID} - {prevResults.PermaLink}");
                            await RedditSharp.Things.ModeratableThing.ReportAsync(agent, updMediaIDResult.AnalysisDetails.ThingID, RedditSharp.Things.ModeratableThing.ReportType.Other, updMediaIDResult.AnalysisDetails.ReportReason).ConfigureAwait(false);
                            await processedDAL.UpdateProcessedPostActionAsync(subreddit, updMediaIDResult.AnalysisDetails.ThingID, updMediaIDResult.AnalysisDetails.MediaID, updMediaIDResult.AnalysisDetails.MediaPlatform, "Report").ConfigureAwait(false);
                        }
                    }
                }
            }


                    
            return;

        }

        private async Task<Models.AnalysisResults> CombineResults( List<Task<Models.AnalysisDetails>> analysisTasks, Models.SubredditSettings settings, string thingid ) {
            var results = new Models.AnalysisResults();
            results.AnalysisDetails.ThingID = thingid;
            results.AnalysisDetails.ThingType = thingid.ToLower().StartsWith("t3_") ? Models.AnalyzableTypes.Post : Models.AnalyzableTypes.Comment;

            while(analysisTasks.Count > 0) {
                var finishedTask = await Task.WhenAny(analysisTasks);
                analysisTasks.Remove(finishedTask);
                var result = await finishedTask;

                results.AnalysisDetails.Scores.AddRange(result.Scores);
                results.AnalysisDetails.AnalyzingModule = result.AnalyzingModule | results.AnalysisDetails.AnalyzingModule;
            }
            if(results.AnalysisDetails.TotalScore >= settings.RemoveScoreThreshold && settings.RemoveScoreThreshold > 0) {
                results.RequiredAction = Models.AnalysisResults.Action.Remove;
            }
            else if(results.AnalysisDetails.TotalScore >= settings.ReportScoreThreshold && settings.ReportScoreThreshold > 0) {
                results.RequiredAction = Models.AnalysisResults.Action.Report;
            }
            else {
                results.RequiredAction = Models.AnalysisResults.Action.None;
            }
            return results;
        }

        private async Task<Dictionary<string, List<Models.AnalysisResults>>> CombineResults(List<Task<Dictionary<string, Models.AnalysisDetails>>> analysisTasks, Models.SubredditSettings settings) {
            var toReturn = new Dictionary<string, List<Models.AnalysisResults>>();

            while(analysisTasks.Count > 0) {
                var finishedTask = await Task.WhenAny(analysisTasks).ConfigureAwait(false);
                analysisTasks.Remove(finishedTask);
                var result = await finishedTask;

                foreach(string thingid in result.Keys) {
                    List<Models.AnalysisResults> results;
                    var details = result[thingid];
                    if(!toReturn.TryGetValue(thingid, out results)) {
                        results = new List<Models.AnalysisResults>();
                        toReturn.Add(thingid, results);
                    }
                    Models.AnalysisResults mediaIDResult = results.SingleOrDefault(r=>r.AnalysisDetails.MediaID == details.MediaID & r.AnalysisDetails.MediaPlatform == details.MediaPlatform);
                    if(mediaIDResult == null) {
                        mediaIDResult = new Models.AnalysisResults();
                        mediaIDResult.AnalysisDetails = details;
                    }
                    results.Add(mediaIDResult);
                    //results.AnalysisDetails.ThingID = thingid;
                    //results.AnalysisDetails.ThingType = thingid.ToLower().StartsWith("t3_") ? Models.AnalyzableTypes.Post : Models.AnalyzableTypes.Comment;
                    //results.AnalysisDetails.Scores.AddRange(details.Scores);
                    //results.AnalysisDetails.AnalyzingModule = results.AnalysisDetails.AnalyzingModule | details.AnalyzingModule;

                   
                }
            }

            return toReturn;
        }

        public async Task<Models.AnalysisResults> AnalyzeMedia(string subreddit, Models.AnalysisRequest request, bool actOnInfo = true ) {

            logger.LogInformation($"Analyzing: {subreddit} : {request.ThingID}");
            var settings = await subSetsBLL.GetSubredditSettingsAsync(subreddit).ConfigureAwait(false);
            if(settings == null) {
                logger.LogInformation($"{subreddit} does not have settings or dirtbag enabled");
                return null;
            }
            return await AnalyzeMedia(settings, request, actOnInfo);
        }
        public async Task<Models.AnalysisResults> AnalyzeMedia(Models.SubredditSettings settings, Models.AnalysisRequest request, bool actOnInfo = true)
        {
            List<Task<Models.AnalysisDetails>> analysisTasks = new List<Task<Models.AnalysisDetails>>();
            if(settings.LicensingSmasher.Enabled) {
                analysisTasks.Add(new Modules.LicensingSmasher(config, settings.LicensingSmasher, settings.Subreddit).Analyze(request));
            }
            if(settings.SelfPromotionCombustor.Enabled) {
                analysisTasks.Add(new Modules.SelfPromotionCombustor(config, settings.SelfPromotionCombustor, postHistoryDAL).Analyze(request));
            }
            if(settings.YouTubeSpamDetector.Enabled) {
                analysisTasks.Add(new Modules.YouTubeSpamDetector(config, settings.YouTubeSpamDetector, settings.Subreddit).Analyze(request));
            }


            var results = await CombineResults(analysisTasks, settings, request.ThingID).ConfigureAwait(false);
            logger.LogInformation($"Analysis Complete: {settings.Subreddit} : {request.ThingID} : {results.AnalysisDetails.TotalScore} - {results.RequiredAction}");
            Models.ProcessedItem item = new Models.ProcessedItem(settings.Subreddit, request.ThingID, request.Author.Name, results.RequiredAction.ToString(), request.PermaLink, request.MediaID, request.MediaChannelID, request.MediaChannelName, request.MediaPlatform, results.AnalysisDetails,results.AnalysisDetails.AnalyzingModule);

            if(actOnInfo) {
                await processedDAL.LogProcessedItemAsync(item).ConfigureAwait(false);
            }

            if(actOnInfo && botAgentPool != null && results.RequiredAction != Models.AnalysisResults.Action.None && (string.IsNullOrWhiteSpace(config["SkipBotActions"]) || config["SkipBotActions"].ToLower() == "false")) {
                var agent = await botAgentPool.GetOrCreateAgentAsync(settings.BotName, () => {
                    logger.LogInformation("Creating web agent for " + settings.BotName);
                    var toReturn = new RedditSharp.BotWebAgent(settings.BotName, settings.BotPass, settings.BotAppID, settings.BotAppSecret, null);
                    toReturn.RateLimiter = new RedditSharp.RateLimitManager(RedditSharp.RateLimitMode.SmallBurst);
                    return Task.FromResult(toReturn);
                });
                
                if(results.RequiredAction == Models.AnalysisResults.Action.Remove) {
                    logger.LogInformation($"Removing thing {results.AnalysisDetails.ThingID} - {request.PermaLink}");
                    await RedditSharp.Things.VotableThing.RemoveAsync(agent, results.AnalysisDetails.ThingID).ConfigureAwait(false);
                    if(results.AnalysisDetails.ThingType == Models.AnalyzableTypes.Post && results.AnalysisDetails.HasFlair) {
                        await RedditSharp.Things.Post.SetFlairAsync(agent, settings.Subreddit, results.AnalysisDetails.ThingID, results.AnalysisDetails.FlairText, results.AnalysisDetails.FlairClass).ConfigureAwait(false);
                    }
                }
                else if(results.RequiredAction == Models.AnalysisResults.Action.Report) {

                    logger.LogInformation($"Reporting thing {results.AnalysisDetails.ThingID} - {request.PermaLink}");
                    await RedditSharp.Things.VotableThing.ReportAsync(agent, results.AnalysisDetails.ThingID, RedditSharp.Things.VotableThing.ReportType.Other, results.AnalysisDetails.ReportReason).ConfigureAwait(false);
                }
            }

            return results;
        }
    }
}
