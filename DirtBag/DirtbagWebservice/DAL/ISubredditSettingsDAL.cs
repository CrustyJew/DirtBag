using System.Threading.Tasks;

namespace DirtBagWebservice.DAL {
    public interface ISubredditSettingsDAL {
        Task<Models.SubredditSettings> GetSubredditSettingsAsync( string subreddit );
    }
}