using System.Threading.Tasks;

namespace DirtBag.DAL {
    public interface ISubredditSettingsDAL {
        Task<Models.SubredditSettings> GetSubredditSettingsAsync( string subreddit );
    }
}