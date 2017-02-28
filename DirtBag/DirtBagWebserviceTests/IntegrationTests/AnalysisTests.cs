using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;

namespace DirtBagWebserviceTests.IntegrationTests
{
    public class AnalysisTests
    {
        private IConfigurationRoot config;
        private DirtBagWebservice.Models.SubredditSettings testSettings;
        public AnalysisTests() {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<SubredditSettingsTest>();
            config = builder.Build();

            testSettings = new DirtBagWebservice.Models.SubredditSettings {
                LastModified = DateTime.Parse( "2017-02-25 14:24:00PM" ),
                ModifiedBy = "TestUser",
                RemoveScoreThreshold = 10,
                ReportScoreThreshold = 5,
                Subreddit = "testsubbie",
                LicensingSmasher = new DirtBagWebservice.Models.LicensingSmasherSettings {
                    Enabled = true,
                    KnownLicensers = new Dictionary<string, string> { { "H7XeNNPkVV3JZxXm-O-MCA", "Jukin Media" } },
                    MatchTerms = new List<string> { "mtashed" },
                    RemovalFlair = new DirtBagWebservice.Models.Flair( "Licensed", "red", 1 ),
                    ScoreMultiplier = 2
                },
                SelfPromotionCombustor = new DirtBagWebservice.Models.SelfPromotionCombustorSettings {
                    Enabled = true,
                    ScoreMultiplier = 2,
                    GracePeriod = 3,
                    RemovalFlair = new DirtBagWebservice.Models.Flair( "10%", "red", 2 ),
                    IncludePostInPercentage = false,
                    PercentageThreshold = 10
                },
                YouTubeSpamDetector = new DirtBagWebservice.Models.YouTubeSpamDetectorSettings()
            };
            testSettings.YouTubeSpamDetector.SetDefaultSettings();
            testSettings.YouTubeSpamDetector.Enabled = true;
            testSettings.YouTubeSpamDetector.ScoreMultiplier = 2;
        }
        [Fact]
        public async Task AnalyzeItem() {
            var userPostHistory = new List<DirtBagWebservice.Models.UserPostInfo> {new DirtBagWebservice.Models.UserPostInfo {
                MediaChannelID = "UCsVXjNRWJMyXViNLM2pyMfg",
                MediaPlatform = DirtBagWebservice.Models.VideoProvider.YouTube,
                ThingID = "t3_666", Username="testuser", MediaAuthor ="Jukin Media", MediaUrl="https://test.com" } };

            var subSettings = new Mock<DirtBagWebservice.BLL.ISubredditSettingsBLL>();
            subSettings.Setup( s => s.GetSubredditSettingsAsync( It.IsAny<string>(), It.IsAny<bool>() ) )
                .Returns( Task.FromResult( testSettings ) );

            var postHistory = new Mock<DirtBagWebservice.DAL.IUserPostingHistoryDAL>();
            postHistory.Setup( p => p.GetUserPostingHistoryAsync( It.IsAny<string>() ) )
                .Returns( Task.FromResult<IEnumerable<DirtBagWebservice.Models.UserPostInfo>>( userPostHistory ) );

            var request = new DirtBagWebservice.Models.AnalysisRequest() {
                Author = new DirtBagWebservice.Models.AuthorInfo { Name = "testuser", CommentKarma = 5, LinkKarma = 5, Created = DateTime.UtcNow },
                EntryTime = DateTime.UtcNow,
                MediaID = "OcWH9Rvp3l4",
                MediaChannelID = "UCsVXjNRWJMyXViNLM2pyMfg",
                MediaChannelName = "Jukin Media",
                MediaPlatform = DirtBagWebservice.Models.VideoProvider.YouTube,
                ThingID = "t3_666"
            };

            var bll = new  DirtBagWebservice.BLL.AnalyzePostBLL(config,subSettings.Object, postHistory.Object);

            var results = await bll.AnalyzePost( "testsubbie", request );

            Assert.NotNull( results );
            Assert.Equal( "t3_666", results.AnalysisDetails.ThingID );
            Assert.Equal( DirtBagWebservice.Models.AnalysisResults.Action.Remove, results.RequiredAction );
            Assert.True( results.AnalysisDetails.HasFlair );
            Assert.Equal( "Licensed", results.AnalysisDetails.FlairText );
            Assert.Equal( "red", results.AnalysisDetails.FlairClass );
        }
    }
}
