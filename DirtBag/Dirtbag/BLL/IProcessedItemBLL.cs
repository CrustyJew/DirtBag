using System.Threading.Tasks;
using Dirtbag.Models;
using System.Collections.Generic;

namespace Dirtbag.BLL
{
    public interface IProcessedItemBLL
    {
        Task<AnalysisResponse> ReadThingAnalysis( string thingID, string subreddit);
    }
}