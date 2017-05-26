using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.BLL
{
    public interface IProcessedPostBLL
    {
        Task<ProcessedItem> ReadProcessedPost(string thingID, string subreddit);
    }
}