using System.Threading.Tasks;
using DirtBagWebservice.Models;

namespace DirtBagWebservice.BLL {
    public interface ISubredditSettingsBLL {
        Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit, bool defaults = false );
        void PurgeSubSettingsFromCache( string subreddit );
        Task SetSubredditSettingsAsync( SubredditSettings settings, string modifiedBy );
    }
}