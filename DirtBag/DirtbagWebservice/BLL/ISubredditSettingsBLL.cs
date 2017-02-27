using System.Threading.Tasks;
using DirtBagWebservice.Models;

namespace DirtBagWebservice.BLL {
    public interface ISubredditSettingsBLL {
        Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit, bool defaults );
        void PurgeSubSettingsFromCache( string subreddit );
    }
}