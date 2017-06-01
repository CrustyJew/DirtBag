using System.Threading.Tasks;
using DirtbagWebservice.Models;
using System.Collections.Generic;

namespace DirtbagWebservice.BLL
{
    public interface IProcessedItemBLL
    {
        Task<IEnumerable<ProcessedItem>> ReadProcessedPost( string thingID, string subreddit);
    }
}