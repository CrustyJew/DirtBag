using System.Collections.Generic;
using System.Threading.Tasks;
using Dirtbag.Models;

namespace Dirtbag.DAL {
    public interface IProcessedItemDAL {
        Task LogProcessedItemAsync( ProcessedItem processed );
        Task<ProcessedItem> ReadProcessedItemAsync( string thingID, string subName );
        Task<AnalysisResponse> GetThingAnalysis( string thingID, string subName );
        Task<IEnumerable<AnalysisResponse>> GetThingsAnalysis( IEnumerable<string> thingIDs, string subName );
        Task<IEnumerable<ProcessedItem>> ReadProcessedItemsAsync( IEnumerable<string> thingIDs, string subName );
        Task UpdatedAnalysisScoresAsync( string subName, string thingID, string mediaID, Models.VideoProvider mediaPlatform, IEnumerable<Models.AnalysisScore> scores, string updateRequestor );
    }
}