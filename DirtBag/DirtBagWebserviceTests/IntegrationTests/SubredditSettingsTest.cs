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

[assembly: UserSecretsId("aspnet-DirtBagWebserviceTests-20170223045757")]
namespace DirtBagWebserviceTests.IntegrationTests
{
    
    public class SubredditSettingsTest
    {
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
            var dal = new DirtBagWebservice.DAL.SubredditSettingsPostgresDAL(conn);
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new DirtBagWebservice.BLL.SubredditSettingsBLL( dal, cache );

            var settings = await bll.GetSubredditSettingsAsync( "thissubdoesntexist", true );

            var expected = DirtBagWebservice.Models.SubredditSettings.GetDefaultSettings();
            expected.Subreddit = "thissubdoesntexist";

            Assert.NotNull( settings );
            Assert.Equal( expected.RemoveScoreThreshold, settings.RemoveScoreThreshold );
            Assert.Equal( expected.LicensingSmasher.KnownLicensers.Keys, settings.LicensingSmasher.KnownLicensers.Keys );
        }

        //Please don't judge me for the sins I'm about to commit.
        [Fact]
        public async Task SetSubredditSettingsTest() {
            var conn = new NpgsqlConnection( config.GetConnectionString( "Sentinel" ) );
            var dal = new DirtBagWebservice.DAL.SubredditSettingsPostgresDAL( conn );
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new DirtBagWebservice.BLL.SubredditSettingsBLL( dal, cache );

            var settings = DirtBagWebservice.Models.SubredditSettings.GetDefaultSettings();
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
            settings.YouTubeSpamDetector.RemovalFlair = new DirtBagWebservice.Models.Flair( "testflair", "testclass", 1 );
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
    }
}
