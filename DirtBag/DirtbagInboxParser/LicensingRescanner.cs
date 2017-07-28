using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagInboxParser {
    public class LicensingRescanner {
        private RedditSharp.Reddit unAuthRedditClient;

        public LicensingRescanner( ) {
            unAuthRedditClient = new RedditSharp.Reddit();
        }

        public async Task Rescan() {
            var subSettingsDAL = new Dirtbag.DAL.SubredditSettingsPostgresDAL(new NpgsqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["SentinelDirtbag"].ConnectionString));
            var subs = await subSettingsDAL.GetLicensingSmasherSubredditsAsync();

            List<Task> reAnalysisTasks = new List<Task>();

            foreach(var sub in subs) {
                RedditSharp.Things.Subreddit subreddit;
                try {
                    subreddit = await unAuthRedditClient.GetSubredditAsync(sub);
                }
                catch(RedditSharp.RedditHttpException ex) {
                    if(ex.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                        //private sub, just skip it
                        continue;
                    }
                    else { throw; }
                }

                var hotPosts = await subreddit.GetPosts(RedditSharp.Things.Subreddit.Sort.Hot, 100).ToList();
                var newPosts = await subreddit.GetPosts(RedditSharp.Things.Subreddit.Sort.New, 100).ToList();
                var risingPosts = await subreddit.GetPosts(RedditSharp.Things.Subreddit.Sort.Rising, 100).ToList();

                var postComparer = new Dirtbag.Helpers.PostIdEqualityComparer();
                var allPosts = new HashSet<RedditSharp.Things.Post>(postComparer);
                allPosts.UnionWith(newPosts);
                allPosts.UnionWith(hotPosts);
                allPosts.UnionWith(risingPosts);
                var settingsDAL = new Dirtbag.DAL.SubredditSettingsPostgresDAL(new NpgsqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["SentinelDirtbag"].ConnectionString));
                var settingsBLL = new Dirtbag.BLL.SubredditSettingsBLL(settingsDAL, Program.MemCache);
                var processedItemDAL = new Dirtbag.DAL.ProcessedItemSQLDAL(new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["Dirtbag"].ConnectionString));
                ILoggerFactory loggerFactory = new LoggerFactory();
                var analysisBLL = new Dirtbag.BLL.AnalyzeMediaBLL(Program.ConfigRoot, settingsBLL, null, processedItemDAL, Program.BotAgentPool, loggerFactory.CreateLogger<Dirtbag.BLL.AnalyzeMediaBLL>());
                reAnalysisTasks.Add(analysisBLL.UpdateAnalysisAsync(allPosts.Select(p => p.FullName), sub, "DirtbagLicensingSmasher"));
            }

            await Task.WhenAll(reAnalysisTasks);
        }
    }
}
