using Microsoft.VisualStudio.TestTools.UnitTesting;
using DirtBag.BotFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirtBag.Helpers;

namespace DirtBag.BotFunctions.Tests {
    [TestClass()]
    public class AutoModWranglerTests {
        [TestMethod()]
        public void GetAutomodConfigTest() {
            var Agent = new RedditWebAgent();
            RedditWebAgent.EnableRateLimit = true;
            RedditWebAgent.RateLimit = RedditWebAgent.RateLimitMode.Burst;
            RedditWebAgent.RootDomain = "oauth.reddit.com";
            RedditWebAgent.UserAgent = "DirtBagUnitTests (By meepster23)";
            RedditWebAgent.Protocol = "https";
            var Auth = new RedditAuth( Agent );

            Auth.Login();
            Agent.AccessToken = Auth.AccessToken;

            AutoModWrangler w = new AutoModWrangler( new RedditSharp.Reddit( Agent, false ).GetSubreddit( "GooAway" ) );
            w.GetAutomodConfig();
        }
    }
}