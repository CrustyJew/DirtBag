using DirtBagWebservice.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace DirtBagWebservice.BLL {
    public class SubredditSettingsBLL : ISubredditSettingsBLL {
        private static IMemoryCache cache;
        private const string CACHE_PREFIX = "SubredditSettings:";
        private DAL.ISubredditSettingsDAL dal;

        public SubredditSettingsBLL( DAL.ISubredditSettingsDAL ssDAL, IMemoryCache memCache ) {
            dal = ssDAL;
            cache = memCache;
        }

        public Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit ) {
            return GetOrUpdateSettingsAsync( subreddit );
        }

        public void PurgeSubSettingsFromCache( string subreddit ) {

            cache.Remove( CACHE_PREFIX + subreddit );

        }

        private async Task<SubredditSettings> GetOrUpdateSettingsAsync( string subreddit ) {
            object cacheVal;
            if ( !cache.TryGetValue( CACHE_PREFIX + subreddit, out cacheVal ) ) {
                return (SubredditSettings) cacheVal;
            }
            else {
                var settings = await dal.GetSubredditSettingsAsync( subreddit );
                cache.Set( CACHE_PREFIX + subreddit, settings, DateTimeOffset.Now.AddMinutes( 30 ) );
                return settings;
            }
        }
    }
}
