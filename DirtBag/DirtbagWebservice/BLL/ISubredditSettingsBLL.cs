using System.Threading.Tasks;
using DirtBagWebservice.Models;

namespace DirtBagWebservice.BLL {
    public interface ISubredditSettingsBLL {
        Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit );
        void PurgeSubSettingsFromCache( string subreddit );
    }
}