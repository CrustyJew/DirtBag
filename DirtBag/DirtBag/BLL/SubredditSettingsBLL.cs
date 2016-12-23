using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.BLL {
    class SubredditSettingsBLL {
        private static MemoryCache cache = MemoryCache.Default;
        private const string CACHE_PREFIX = "SubredditSettings:";
        internal Task<BotSettings> GetSubredditSettingsAsync( string subreddit) {
            return GetOrUpdateSettingsAsync( subreddit );
        }

        internal void PurgeSubSettingsFromCache(string subreddit ) {
            if ( cache.Contains( CACHE_PREFIX + subreddit ) ) {
                cache.Remove( CACHE_PREFIX + subreddit );
            }
        }

        private async Task<BotSettings> GetOrUpdateSettingsAsync(string subreddit ) {
            if(cache.Contains(CACHE_PREFIX + subreddit ) ) {
                return (BotSettings) cache[CACHE_PREFIX + subreddit];
            }
            else {
                var settingsDAL = new DAL.SubredditSettingsDAL();
                var settings = await settingsDAL.GetSubredditSettingsAsync( subreddit );
                cache.AddOrGetExisting( CACHE_PREFIX + subreddit, settings, DateTimeOffset.Now.AddMinutes( 30 ) );
                return settings;
            }
        }
    }
}
