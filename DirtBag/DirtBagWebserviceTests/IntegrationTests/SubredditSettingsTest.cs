using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace DirtbagWebserviceTests.IntegrationTests {

    public class SubredditSettingsTest {
        private IConfigurationRoot config;
        private IServiceProvider serviceProvider;
        public SubredditSettingsTest() {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<SubredditSettingsTest>();
            config = builder.Build();
            var servs = new ServiceCollection();
            servs.AddMemoryCache();
            serviceProvider = servs.BuildServiceProvider();

        }
        [Fact]
        public async Task GetDefaultSubredditSettingsTest() {
            var conn = new NpgsqlConnection( config.GetConnectionString( "Sentinel" ) );
            var dal = new DirtbagWebservice.DAL.SubredditSettingsPostgresDAL( conn );
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new DirtbagWebservice.BLL.SubredditSettingsBLL( dal, cache );

            var settings = await bll.GetSubredditSettingsAsync( "thissubdoesntexist", true );

            var expected = DirtbagWebservice.Models.SubredditSettings.GetDefaultSettings();
            expected.Subreddit = "thissubdoesntexist";

            Assert.NotNull( settings );
            Assert.Equal( expected.RemoveScoreThreshold, settings.RemoveScoreThreshold );
            Assert.Equal( expected.LicensingSmasher.KnownLicensers.Keys, settings.LicensingSmasher.KnownLicensers.Keys );
        }

        //Please don't judge me for the sins I'm about to commit.
        [Fact]
        public async Task SetSubredditSettingsTest() {
            var conn = new NpgsqlConnection( config.GetConnectionString( "Sentinel" ) );
            var dal = new DirtbagWebservice.DAL.SubredditSettingsPostgresDAL( conn );
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new DirtbagWebservice.BLL.SubredditSettingsBLL( dal, cache );

            var settings = DirtbagWebservice.Models.SubredditSettings.GetDefaultSettings();
            settings.Subreddit = "thesentinel_dev";

            settings.RemoveScoreThreshold = 5;
            settings.LicensingSmasher.ScoreMultiplier = 99;

            await bll.SetSubredditSettingsAsync( settings, "testuser" );

            var results = await bll.GetSubredditSettingsAsync( "thesentinel_dev" );


            Assert.Equal( settings.LicensingSmasher.ScoreMultiplier, results.LicensingSmasher.ScoreMultiplier );
            Assert.Equal( settings.RemoveScoreThreshold, results.RemoveScoreThreshold );
            Assert.Equal( settings.LicensingSmasher.KnownLicensers.Count, results.LicensingSmasher.KnownLicensers.Count );
            Assert.Equal( settings.LicensingSmasher.KnownLicensers.Keys, settings.LicensingSmasher.KnownLicensers.Keys );

            settings.LicensingSmasher.KnownLicensers = new Dictionary<string, string>();
            settings.LicensingSmasher.KnownLicensers.Add( "testkey", "testvalue" );
            settings.LicensingSmasher.MatchTerms = new List<string>();
            settings.LicensingSmasher.MatchTerms.Add( "testterm" );

            settings.SelfPromotionCombustor.GracePeriod = 1000;
            settings.SelfPromotionCombustor.RemovalFlair = null;
            settings.YouTubeSpamDetector.RemovalFlair = new DirtbagWebservice.Models.Flair( "testflair", "testclass", 1 );
            settings.YouTubeSpamDetector.LicensedChannel.Enabled = false;
            settings.YouTubeSpamDetector.LicensedChannel.Weight = 99;

            await bll.SetSubredditSettingsAsync( settings, "testuser2" );
            results = await bll.GetSubredditSettingsAsync( "thesentinel_dev" );

            Assert.Equal( settings.LicensingSmasher.KnownLicensers, results.LicensingSmasher.KnownLicensers );
            Assert.Equal( settings.LicensingSmasher.MatchTerms, results.LicensingSmasher.MatchTerms );
            Assert.Equal( settings.SelfPromotionCombustor.GracePeriod, results.SelfPromotionCombustor.GracePeriod );
            Assert.Null( results.SelfPromotionCombustor.RemovalFlair );
            Assert.Equal( settings.YouTubeSpamDetector.RemovalFlair.Text, results.YouTubeSpamDetector.RemovalFlair.Text );
            Assert.Equal( settings.YouTubeSpamDetector.RemovalFlair.Class, results.YouTubeSpamDetector.RemovalFlair.Class );
            Assert.Equal( settings.YouTubeSpamDetector.RemovalFlair.Priority, results.YouTubeSpamDetector.RemovalFlair.Priority );
            Assert.Equal( settings.YouTubeSpamDetector.LicensedChannel.Enabled, results.YouTubeSpamDetector.Enabled );
            Assert.Equal( settings.YouTubeSpamDetector.LicensedChannel.Weight, results.YouTubeSpamDetector.LicensedChannel.Weight );
        }
        [Fact]
        public async Task SetVideos() {
            var conn = new NpgsqlConnection( config.GetConnectionString( "Sentinel" ) );
            var dal = new DirtbagWebservice.DAL.SubredditSettingsPostgresDAL( conn );
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new DirtbagWebservice.BLL.SubredditSettingsBLL( dal, cache );

            var settings = DirtbagWebservice.Models.SubredditSettings.GetDefaultSettings();

            settings.Subreddit = "TheSentinel_dev2";
            settings.ReportScoreThreshold = 7.5;
            settings.RemoveScoreThreshold = 10.0;

            settings.LicensingSmasher = new DirtbagWebservice.Models.LicensingSmasherSettings() {
                Enabled = true,
                RemovalFlair = new DirtbagWebservice.Models.Flair {
                    Text = "R10",
                    Class = "red",
                    Priority = 1
                }, ScoreMultiplier = 1.5,
                MatchTerms = new List<string> { "jukin", "licensing", "break.com", "storyful", "rumble", "newsflare", "visualdesk", "viral spiral", "viralspiral", "rightser", "flockvideo", "to use this video in a commercial", "media enquiries", "homevideolicensing", "DefyClassic", "unilad", "bravebison", "swns" },
                KnownLicensers = new Dictionary<string, string> {
                    {"H7XeNNPkVV3JZxXm-O-MCA", "Jukin Media"},
{"Newsflare", "Newsflare"},
{"3339WgBDKIcxTfywuSmG8w", "ViralHog"},
{"Storyful", "Storyful"},
{"rumble", "Rumble"},
{"Rightster_Entertainment_Affillia", "Viral Spiral"},
{"Break", "Break"},
{"RightsterEntertainment", "Viral Spiral"},
{"Fullscreen_VIN", "Fullscreen"},
{"Flock", "Flock"},
{"LetsonCorporationLtd_Affiliate", "Letson"},
{"myvideorights_usa", "Rightser"},
{"DefyClassic", "DefyClassic/Break"},
{"FunnyFuse", "homevideolicensing"},
{"GoProCameraPremium", "GoPro"},
{"base79_affiliate" , "BraveBison / GoPro"},
{"jonmillsswns%2Buser" , "SWNS"}
                }
            };

            settings.YouTubeSpamDetector = new DirtbagWebservice.Models.YouTubeSpamDetectorSettings();
            settings.YouTubeSpamDetector.SetDefaultSettings();
            settings.YouTubeSpamDetector.Enabled = true;
            settings.YouTubeSpamDetector.ScoreMultiplier = 1;

            settings.SelfPromotionCombustor = new DirtbagWebservice.Models.SelfPromotionCombustorSettings() {
                Enabled = true,
                RemovalFlair = new DirtbagWebservice.Models.Flair( "10%", "red", 1 ),
                GracePeriod = 3,
                IncludePostInPercentage = false,
                PercentageThreshold = 10,
                ScoreMultiplier = 2
            };

            await bll.SetSubredditSettingsAsync( settings, "Meepster23" );
        }
    }
}
