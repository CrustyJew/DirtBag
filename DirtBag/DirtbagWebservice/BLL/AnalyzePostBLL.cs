﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagWebservice.BLL {
    public class AnalyzePostBLL : IAnalyzePostBLL {
        private BLL.ISubredditSettingsBLL subSetsBLL;
        private DAL.IUserPostingHistoryDAL postHistoryDAL;
        private IConfigurationRoot config;
        private DAL.IProcessedItemDAL processedDAL;
        public AnalyzePostBLL( IConfigurationRoot config, BLL.ISubredditSettingsBLL settingsBLL, DAL.IUserPostingHistoryDAL userPostHistoryDAL, DAL.IProcessedItemDAL processedItemDAL ) {
            subSetsBLL = settingsBLL;
            postHistoryDAL = userPostHistoryDAL;
            this.config = config;
            processedDAL = processedItemDAL;
        }

        public async Task<Models.ProcessedItem> UpdateAnalysisAsync( string subreddit, string thingID, string mediaID, Models.VideoProvider mediaPlatform, string updateBy ) {
            var previousResults = await processedDAL.ReadProcessedItemAsync(thingID, subreddit, mediaID, mediaPlatform);
            Models.AnalysisRequest request = new Models.AnalysisRequest() {
                MediaChannelID = previousResults.MediaChannelID,
                MediaID = previousResults.MediaID,
                MediaPlatform = previousResults.MediaPlatform,
                ThingID = previousResults.ThingID,
                PermaLink = previousResults.PermaLink
            };
            //Only rerun things that don't need to run at the time a post was made
            var settings = await subSetsBLL.GetSubredditSettingsAsync(subreddit, true);
            List<Task<Models.AnalysisDetails>> analysisTasks = new List<Task<Models.AnalysisDetails>>();
            if(settings.LicensingSmasher.Enabled) {
                analysisTasks.Add(new Modules.LicensingSmasher(config, settings.LicensingSmasher, subreddit).Analyze(request));
            }

            var updatedResults =  await CombineResults(analysisTasks, settings, request.ThingID);

            await processedDAL.UpdatedAnalysisScoresAsync(subreddit, thingID, mediaID, mediaPlatform, updatedResults.AnalysisDetails.Scores, updateBy);

            //easier to just look the stupid thing up again and return
            return await processedDAL.ReadProcessedItemAsync(thingID, subreddit, mediaID, mediaPlatform);

        }

        private async Task<Models.AnalysisResults> CombineResults( List<Task<Models.AnalysisDetails>> analysisTasks, Models.SubredditSettings settings, string thingid ) {
            var results = new Models.AnalysisResults();
            results.AnalysisDetails.ThingID = thingid;

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
                results.RequiredAction = Models.AnalysisResults.Action.Nothing;
            }
            return results;
        }

        public async Task<Models.AnalysisResults> AnalyzePost( string subreddit, Models.AnalysisRequest request ) {
            var settings = await subSetsBLL.GetSubredditSettingsAsync(subreddit, true);
            List<Task<Models.AnalysisDetails>> analysisTasks = new List<Task<Models.AnalysisDetails>>();
            if(settings.LicensingSmasher.Enabled) {
                analysisTasks.Add(new Modules.LicensingSmasher(config, settings.LicensingSmasher, subreddit).Analyze(request));
            }
            if(settings.SelfPromotionCombustor.Enabled) {
                analysisTasks.Add(new Modules.SelfPromotionCombustor(config, settings.SelfPromotionCombustor, postHistoryDAL).Analyze(request));
            }
            if(settings.YouTubeSpamDetector.Enabled) {
                analysisTasks.Add(new Modules.YouTubeSpamDetector(config, settings.YouTubeSpamDetector, subreddit).Analyze(request));
            }


            var results = await CombineResults(analysisTasks, settings, request.ThingID);

            Models.ProcessedItem item = new Models.ProcessedItem(subreddit, request.ThingID, results.RequiredAction.ToString(), request.PermaLink, request.MediaID, request.MediaPlatform);

            try {
                await processedDAL.LogProcessedItemAsync(item);
            }
            catch {
                //ignore logging errors and respond anyway.
            }

            return results;
        }
    }
}
