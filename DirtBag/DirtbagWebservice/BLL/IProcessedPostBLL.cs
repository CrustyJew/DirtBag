using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.BLL
{
    public interface IProcessedItemBLL
    {
        Task<ProcessedItem> ReadProcessedPost( string thingID, string subreddit, string mediaID, Models.VideoProvider mediaPlatform );
    }
}