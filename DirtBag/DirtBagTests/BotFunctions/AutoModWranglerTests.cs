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
        public async Task AddToBanListTest() {

            var Agent = new RedditWebAgent();
            RedditWebAgent.EnableRateLimit = true;
            RedditWebAgent.RateLimit = RedditWebAgent.RateLimitMode.Burst;
            RedditWebAgent.RootDomain = "oauth.reddit.com";
            RedditWebAgent.UserAgent = "DirtBagUnitTests (By meepster23)";
            RedditWebAgent.Protocol = "https";
            var Auth = new RedditAuth( Agent );

            Auth.Login();
            Agent.AccessToken = Auth.AccessToken;

            AutoModWrangler w = new AutoModWrangler( new RedditSharp.Reddit( Agent, true ).GetSubreddit( "GooAway" ) );
            await w.AddToBanList( new List<Models.BannedEntity>() { new Models.BannedEntity() { BanDate = DateTime.UtcNow, BannedBy = "DirtBagTests", BanReason = "DirtBagTests", EntityString = "DirtBagTests", Type= Models.BannedEntity.EntityType.User, SubName = "GooAway", ThingID = "66666" } } );

            var list = await w.GetBannedList();
            int id = list.Where( l => l.ThingID == "66666" && l.BannedBy == "DirtBagTests" ).Select(l=>l.ID).First();
            await w.RemoveFromBanList( id, "DirtBagTests" );
        }
    }
}