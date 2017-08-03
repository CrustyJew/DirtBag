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
            var dal = new Dirtbag.DAL.SubredditSettingsPostgresDAL( conn );
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new Dirtbag.BLL.SubredditSettingsBLL( dal, cache );

            var settings = await bll.GetSubredditSettingsAsync( "thissubdoesntexist", true );

            var expected = Dirtbag.Models.SubredditSettings.GetDefaultSettings();
            expected.Subreddit = "thissubdoesntexist";

            Assert.NotNull( settings );
            Assert.Equal( expected.RemoveScoreThreshold, settings.RemoveScoreThreshold );
            Assert.Equal( expected.LicensingSmasher.KnownLicensers.Keys, settings.LicensingSmasher.KnownLicensers.Keys );
        }

        //Please don't judge me for the sins I'm about to commit.
        [Fact]
        public async Task SetSubredditSettingsTest() {
            var conn = new NpgsqlConnection( config.GetConnectionString( "Sentinel" ) );
            var dal = new Dirtbag.DAL.SubredditSettingsPostgresDAL( conn );
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new Dirtbag.BLL.SubredditSettingsBLL( dal, cache );

            var settings = Dirtbag.Models.SubredditSettings.GetDefaultSettings();
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
            settings.YouTubeSpamDetector.RemovalFlair = new Dirtbag.Models.Flair( "testflair", "testclass", 1 );
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
        public void TestDeserialization() {
            string json = @"{""subreddit"":""videos"",""reportScoreThreshold"":7.5,""removeScoreThreshold"":10,""lastModified"":""2017 - 03 - 06T16: 38:25.56692"",""modifiedBy"":""Meepster23"",""licensingSmasher"":{ ""enabled"":true,""removalFlair"":{ ""text"":""R10"",""class"":""red"",""priority"":1,""enabled"":true},""matchTerms"":[""licensing"",""break.com"",""storyful"",""rumble"",""newsflare"",""visualdesk"",""viral spiral"",""viralspiral"",""rightser"",""flockvideo"",""to use this video in a commercial"",""media enquiries"",""homevideolicensing"",""DefyClassic"",""unilad"",""bravebison"",""swns"",""jukin""],""knownLicensers"":{""jonmillsswns%2Buser"":""SWNS"",""base79_affiliate"":""BraveBison / GoPro"",""GoProCameraPremium"":""GoPro"",""FunnyFuse"":""homevideolicensing"",""DefyClassic"":""DefyClassic/Break"",""myvideorights_usa"":""Rightser"",""LetsonCorporationLtd_Affiliate"":""Letson"",""Flock"":""Flock"",""Fullscreen_VIN"":""Fullscreen"",""RightsterEntertainment"":""Viral Spiral"",""Break"":""Break"",""Rightster_Entertainment_Affillia"":""Viral Spiral"",""rumble"":""Rumble"",""Storyful"":""Storyful"",""3339WgBDKIcxTfywuSmG8w"":""ViralHog"",""Newsflare"":""Newsflare"",""H7XeNNPkVV3JZxXm-O-MCA"":""Jukin Media""},""scoreMultiplier"":""2.5"",""lastModified"":""2017-07-06T20:30:05.116653"",""modifiedBy"":""Meepster23""},""youTubeSpamDetector"":{""enabled"":true,""scoreMultiplier"":1,""lastModified"":""2017-05-30T13:42:48.026874"",""modifiedBy"":""Meepster23"",""removalFlair"":{""text"":null,""class"":null,""priority"":0,""enabled"":false},""channelAgeThreshold"":{""name"":""ChannelAgeThreshold"",""value"":60,""enabled"":true,""weight"":3},""viewCountThreshold"":{""name"":""ViewCountThreshold"",""value"":300,""enabled"":true,""weight"":1},""voteCountThreshold"":{""name"":""VoteCountThreshold"",""value"":25,""enabled"":true,""weight"":1},""negativeVoteRatio"":{""name"":""NegativeVoteRatio"",""value"":0,""enabled"":false,""weight"":1},""redditAccountAgeThreshold"":{""name"":""RedditAccountAgeThreshold"",""value"":180,""enabled"":true,""weight"":3},""licensedChannel"":{""name"":""LicensedChannel"",""value"":0,""enabled"":false,""weight"":1},""channelSubscribersThreshold"":{""name"":""ChannelSubscribersThreshold"",""value"":25,""enabled"":true,""weight"":1},""commentCountThreshold"":{""name"":""CommentCountThreshold"",""value"":10,""enabled"":true,""weight"":1}},""selfPromotionCombustor"":{""enabled"":true,""removalFlair"":{""text"":""10%"",""class"":""red"",""priority"":1,""enabled"":true},""scoreMultiplier"":2,""percentageThreshold"":10,""includePostInPercentage"":false,""gracePeriod"":3,""lastModified"":""2017-07-06T20:58:38.399542"",""modifiedBy"":""Meepster23""}}";
            Dirtbag.Models.SubredditSettings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dirtbag.Models.SubredditSettings>(json);

            Assert.NotNull(settings);
            Assert.NotNull(settings.Subreddit);
        }
    }
}
