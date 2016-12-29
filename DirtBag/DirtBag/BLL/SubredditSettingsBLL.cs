using DirtBag.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.BLL {
    public class SubredditSettingsBLL {
        private static MemoryCache cache = MemoryCache.Default;
        private const string CACHE_PREFIX = "SubredditSettings:";
        private DAL.ISubredditSettingsDAL dal;

        public SubredditSettingsBLL(DAL.ISubredditSettingsDAL ssDAL ) {
            dal = ssDAL;
        }

        public Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit) {
            return GetOrUpdateSettingsAsync( subreddit );
        }

        public void PurgeSubSettingsFromCache(string subreddit ) {
            if ( cache.Contains( CACHE_PREFIX + subreddit ) ) {
                cache.Remove( CACHE_PREFIX + subreddit );
            }
        }

        private async Task<SubredditSettings> GetOrUpdateSettingsAsync(string subreddit ) {
            if(cache.Contains(CACHE_PREFIX + subreddit ) ) {
                return (SubredditSettings) cache[CACHE_PREFIX + subreddit];
            }
            else {
                var settings = await dal.GetSubredditSettingsAsync( subreddit );
                cache.AddOrGetExisting( CACHE_PREFIX + subreddit, settings, DateTimeOffset.Now.AddMinutes( 30 ) );
                return settings;
            }
        }
    }
}
