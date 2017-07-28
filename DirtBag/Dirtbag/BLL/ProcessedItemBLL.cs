using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dirtbag.BLL {
    public class ProcessedItemBLL : IProcessedItemBLL
    {
        private DAL.IProcessedItemDAL dal;
        public ProcessedItemBLL(DAL.IProcessedItemDAL ppDAL ) {
            dal = ppDAL;
        }

        public Task<Models.AnalysisResponse> ReadThingAnalysis(string thingID, string subreddit) {
            return dal.GetThingAnalysis( thingID, subreddit );
        }
    }
}
