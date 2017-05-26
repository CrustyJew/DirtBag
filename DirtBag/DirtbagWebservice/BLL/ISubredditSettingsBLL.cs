using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.BLL {
    public interface ISubredditSettingsBLL {
        Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit, bool defaults = false );
        void PurgeSubSettingsFromCache( string subreddit );
        Task SetSubredditSettingsAsync( SubredditSettings settings, string modifiedBy );
    }
}