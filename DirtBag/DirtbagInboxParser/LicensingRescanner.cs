using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagInboxParser {
    public class LicensingRescanner {
        private Dirtbag.DAL.ISubredditSettingsDAL subSettingsDAL;
        private RedditSharp.Reddit unAuthRedditClient;
        public LicensingRescanner(Dirtbag.DAL.ISubredditSettingsDAL subSettingsDAL ) {
            this.subSettingsDAL = subSettingsDAL;
            unAuthRedditClient = new RedditSharp.Reddit();
        }

        public async Task Rescann() {
            var subs = await subSettingsDAL.GetLicensingSmasherSubredditsAsync();

            List<Task> reAnalysisTasks = new List<Task>();

            foreach(var sub in subs) {
                var subreddit = await unAuthRedditClient.GetSubredditAsync(sub);


            }
        }
    }
}
