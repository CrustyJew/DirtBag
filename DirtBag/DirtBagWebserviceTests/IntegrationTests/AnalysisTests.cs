﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace DirtbagWebserviceTests.IntegrationTests
{
    public class AnalysisTests
    {
        private IConfigurationRoot config;
        private DirtbagWebservice.Models.SubredditSettings testSettings;
        public AnalysisTests() {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<AnalysisTests>();
            config = builder.Build();

            testSettings = new DirtbagWebservice.Models.SubredditSettings {
                LastModified = DateTime.Parse( "2017-02-25 14:24:00PM" ),
                //ModifiedBy = "TestUser",
                RemoveScoreThreshold = 10,
                ReportScoreThreshold = 5,
                Subreddit = "testsubbie",
                LicensingSmasher = new DirtbagWebservice.Models.LicensingSmasherSettings {
                    Enabled = true,
                    KnownLicensers = new Dictionary<string, string> { { "H7XeNNPkVV3JZxXm-O-MCA", "Jukin Media" } },
                    MatchTerms = new List<string> { "mtashed" },
                    RemovalFlair = new DirtbagWebservice.Models.Flair( "Licensed", "red", 1 ),
                    ScoreMultiplier = 2
                },
                SelfPromotionCombustor = new DirtbagWebservice.Models.SelfPromotionCombustorSettings {
                    Enabled = true,
                    ScoreMultiplier = 2,
                    GracePeriod = 3,
                    RemovalFlair = new DirtbagWebservice.Models.Flair( "10%", "red", 2 ),
                    IncludePostInPercentage = false,
                    PercentageThreshold = 10
                },
                YouTubeSpamDetector = new DirtbagWebservice.Models.YouTubeSpamDetectorSettings()
            };
            testSettings.YouTubeSpamDetector.SetDefaultSettings();
            testSettings.YouTubeSpamDetector.Enabled = true;
            testSettings.YouTubeSpamDetector.ScoreMultiplier = 2;
        }
        [Fact]
        public async Task AnalyzeItem() {
            var userPostHistory = new List<DirtbagWebservice.Models.UserPostInfo> {new DirtbagWebservice.Models.UserPostInfo {
                MediaChannelID = "UCsVXjNRWJMyXViNLM2pyMfg",
                MediaPlatform = DirtbagWebservice.Models.VideoProvider.YouTube,
                ThingID = "t3_666", Username="testuser", MediaAuthor ="Jukin Media", MediaUrl="https://test.com" } };

            var subSettings = new Mock<DirtbagWebservice.BLL.ISubredditSettingsBLL>();
            subSettings.Setup( s => s.GetSubredditSettingsAsync( It.IsAny<string>(), It.IsAny<bool>() ) )
                .Returns( Task.FromResult( testSettings ) );

            var postHistory = new Mock<DirtbagWebservice.DAL.IUserPostingHistoryDAL>();
            postHistory.Setup( p => p.GetUserPostingHistoryAsync( It.IsAny<string>() ) )
                .Returns( Task.FromResult<IEnumerable<DirtbagWebservice.Models.UserPostInfo>>( userPostHistory ) );

            var processedDAL = new Mock<DirtbagWebservice.DAL.IProcessedItemDAL>();
            processedDAL.Setup(p => p.LogProcessedItemAsync(It.IsAny<DirtbagWebservice.Models.ProcessedItem>()))
                .Returns(Task.FromResult(0));

            var request = new DirtbagWebservice.Models.AnalysisRequest() {
                Author = new DirtbagWebservice.Models.AuthorInfo { Name = "testuser", CommentKarma = 5, LinkKarma = 5, Created = DateTime.UtcNow },
                EntryTime = DateTime.UtcNow,
                MediaID = "OcWH9Rvp3l4",
                MediaChannelID = "UCsVXjNRWJMyXViNLM2pyMfg",
                MediaChannelName = "Jukin Media",
                MediaPlatform = DirtbagWebservice.Models.VideoProvider.YouTube,
                PermaLink = "https://reddit.com",
                ThingID = "t3_666"
            };

            var bll = new  DirtbagWebservice.BLL.AnalyzeMediaBLL(config,subSettings.Object, postHistory.Object, new DirtbagWebservice.DAL.ProcessedItemSQLDAL(new SqlConnection(config.GetConnectionString("Dirtbag"))), null, new LoggerFactory().CreateLogger<DirtbagWebservice.BLL.AnalyzeMediaBLL>());

            var results = await bll.AnalyzeMedia( "testsubbie", request, false );

            Assert.NotNull( results );
            Assert.Equal( "t3_666", results.AnalysisDetails.ThingID );
            Assert.Equal( DirtbagWebservice.Models.AnalysisResults.Action.Remove, results.RequiredAction );
            Assert.True( results.AnalysisDetails.HasFlair );
            Assert.Equal( "Licensed", results.AnalysisDetails.FlairText );
            Assert.Equal( "red", results.AnalysisDetails.FlairClass );
        }
        [Fact]
        public async Task AnalysisTestAttributionLink() {
            var subSettings = new Mock<DirtbagWebservice.BLL.ISubredditSettingsBLL>();
            subSettings.Setup(s => s.GetSubredditSettingsAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(testSettings));

            var processedDAL = new Mock<DirtbagWebservice.DAL.IProcessedItemDAL>();
            processedDAL.Setup(p => p.LogProcessedItemAsync(It.IsAny<DirtbagWebservice.Models.ProcessedItem>()))
                .Returns(Task.FromResult(0));

            var request = new DirtbagWebservice.Models.AnalysisRequest() {
                Author = new DirtbagWebservice.Models.AuthorInfo { Name = "redhans", CommentKarma = 0, LinkKarma = 1, Created = DateTime.UtcNow },
                EntryTime = DateTime.UtcNow,
                MediaID = "r251n4oPe3Q",
                MediaChannelID = "UCYzz2SkhAaM0FDKuGk-IPZg",
                MediaChannelName = "Richard Aguilar",
                MediaPlatform = DirtbagWebservice.Models.VideoProvider.YouTube,
                PermaLink = "https://redd.it/6dz6lv",
                ThingID = "t3_6dz6lv"
            };
            var bll = new DirtbagWebservice.BLL.AnalyzeMediaBLL(config, subSettings.Object, new DirtbagWebservice.DAL.UserPostingHistoryDAL(new Npgsql.NpgsqlConnection(config.GetConnectionString("SentinelDirtbag"))), new DirtbagWebservice.DAL.ProcessedItemSQLDAL(new SqlConnection(config.GetConnectionString("Dirtbag"))), null, new LoggerFactory().CreateLogger<DirtbagWebservice.BLL.AnalyzeMediaBLL>());

            var results = await bll.AnalyzeMedia("testsubbie", request, false);

            //var dal = new DirtbagWebservice.DAL.UserPostingHistoryDAL(new Npgsql.NpgsqlConnection(config.GetConnectionString("SentinelDirtbag")));
            //var results = await dal.TestUserPostingHistoryAsync("redhans");
        }
    }
}
