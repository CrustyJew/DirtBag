﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.DAL {
    public interface IProcessedItemDAL {
        Task LogProcessedItemAsync( ProcessedItem processed );
        Task<ProcessedItem> ReadProcessedItemAsync( string thingID, string subName );
        Task<IEnumerable<ProcessedItem>> ReadProcessedItemsAsync( IEnumerable<string> thingIDs, string subName );
        Task UpdatedAnalysisScoresAsync( string thingID, string subName, IEnumerable<AnalysisScore> scores );
    }
}