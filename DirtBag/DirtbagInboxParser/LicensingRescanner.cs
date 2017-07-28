using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagInboxParser {
    public class LicensingRescanner {
        private Dirtbag.DAL.ISubredditSettingsDAL subSettingsDAL;
        private RedditSharp.Reddit unAuthRedditClient;
        private Dirtbag.BLL.IAnalyzeMediaBLL analysisBLL;
        public LicensingRescanner(Dirtbag.DAL.ISubredditSettingsDAL subSettingsDAL, Dirtbag.BLL.IAnalyzeMediaBLL analysisBLL ) {
            this.subSettingsDAL = subSettingsDAL;
            unAuthRedditClient = new RedditSharp.Reddit();
            this.analysisBLL = analysisBLL;
        }

        public async Task Rescann() {
            var subs = await subSettingsDAL.GetLicensingSmasherSubredditsAsync();

            List<Task> reAnalysisTasks = new List<Task>();

            foreach(var sub in subs) {
                var subreddit = await unAuthRedditClient.GetSubredditAsync(sub);

                var hotPosts = await subreddit.GetPosts(RedditSharp.Things.Subreddit.Sort.Hot, 100).ToList();
                var newPosts = await subreddit.GetPosts(RedditSharp.Things.Subreddit.Sort.New, 100).ToList();
                var risingPosts = await subreddit.GetPosts(RedditSharp.Things.Subreddit.Sort.Rising, 100).ToList();

                var postComparer = new Dirtbag.Helpers.PostIdEqualityComparer();
                var allPosts = new HashSet<RedditSharp.Things.Post>(postComparer);
                allPosts.UnionWith(newPosts);
                allPosts.UnionWith(hotPosts);
                allPosts.UnionWith(risingPosts);

                reAnalysisTasks.Add(analysisBLL.UpdateAnalysisAsync(allPosts.Select(p => p.FullName), sub, "DirtbagLicensingSmasher"));
            }

            await Task.WhenAll(reAnalysisTasks);
        }
    }
}
