using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBagWebservice.BLL
{
    public class AnalyzePostBLL
    {
        private BLL.SubredditSettingsBLL subSetsBLL;
        private DAL.IUserPostingHistoryDAL postHistoryDAL;
        private IConfigurationRoot config;
        public AnalyzePostBLL(IConfigurationRoot config, BLL.SubredditSettingsBLL settingsBLL, DAL.IUserPostingHistoryDAL userPostHistoryDAL)
        {
            subSetsBLL = settingsBLL;
            postHistoryDAL = userPostHistoryDAL;
            this.config = config;
        }

        public async Task<Models.AnalysisResults> AnalyzePost(string subreddit, Models.AnalysisRequest request)
        {
            var settings = await subSetsBLL.GetSubredditSettingsAsync(subreddit);
            List<Task<Models.AnalysisDetails>> analysisTasks = new List<Task<Models.AnalysisDetails>>();
            if (settings.LicensingSmasher.Enabled)
            {
                analysisTasks.Add(new Modules.LicensingSmasher(config, settings.LicensingSmasher, subreddit).Analyze(request));
            }
            if (settings.SelfPromotionCombustor.Enabled)
            {
                analysisTasks.Add(new Modules.SelfPromotionCombustor( config, settings.SelfPromotionCombustor, postHistoryDAL).Analyze(request));
            }
            if (settings.YouTubeSpamDetector.Enabled)
            {
                analysisTasks.Add(new Modules.YouTubeSpamDetector( config, settings.YouTubeSpamDetector, subreddit).Analyze(request));
            }


            var results = new Models.AnalysisResults();

            while (analysisTasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(analysisTasks);
                analysisTasks.Remove(finishedTask);
                var result = await finishedTask;

                results.AnalysisDetails.Scores.AddRange(result.Scores);
                results.AnalysisDetails.AnalyzingModule = result.AnalyzingModule | results.AnalysisDetails.AnalyzingModule;
            }
            if(results.AnalysisDetails.TotalScore >= settings.RemoveScoreThreshold)
            {
                results.RequiredAction = Models.AnalysisResults.Action.Remove;
            }
            else if (results.AnalysisDetails.TotalScore >= settings.ReportScoreThreshold)
            {
                results.RequiredAction = Models.AnalysisResults.Action.Report;
            }
            else
            {
                results.RequiredAction = Models.AnalysisResults.Action.Nothing;
            }
            return results;
        }
    }
}
