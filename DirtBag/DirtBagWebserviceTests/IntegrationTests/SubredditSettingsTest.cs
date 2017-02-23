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
        public async Task GetSubredditSettingsTest() {
            var conn = new NpgsqlConnection( config.GetConnectionString( "Sentinel" ) );
            var dal = new DirtBagWebservice.DAL.SubredditSettingsPostgresDAL(conn);
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();
            var bll = new DirtBagWebservice.BLL.SubredditSettingsBLL( dal, cache );

            var settings = await bll.GetSubredditSettingsAsync( "thissubdoesntexist" );

            var expected = DirtBagWebservice.Models.SubredditSettings.GetDefaultSettings();
            expected.Subreddit = "thissubdoesntexist";

            Assert.NotNull( settings );
            Assert.Equal( expected.RemoveScoreThreshold, settings.RemoveScoreThreshold );
            Assert.Equal( expected.LicensingSmasher.KnownLicensers.Keys, settings.LicensingSmasher.KnownLicensers.Keys );
        }
    }
}
