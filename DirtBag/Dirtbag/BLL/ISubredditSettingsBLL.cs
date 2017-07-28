using System.Threading.Tasks;
using Dirtbag.Models;

namespace Dirtbag.BLL {
    public interface ISubredditSettingsBLL {
        Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit, bool defaults = false );
        void PurgeSubSettingsFromCache( string subreddit );
        Task SetSubredditSettingsAsync( SubredditSettings settings, string modifiedBy );
    }
}