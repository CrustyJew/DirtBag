using System.Collections.Generic;
using System.Threading.Tasks;

namespace DirtbagWebservice.BLL {
    public class ProcessedItemBLL : IProcessedItemBLL
    {
        private DAL.IProcessedItemDAL dal;
        public ProcessedItemBLL(DAL.IProcessedItemDAL ppDAL ) {
            dal = ppDAL;
        }

        public Task<IEnumerable<Models.ProcessedItem>> ReadProcessedPost(string thingID, string subreddit) {
            return dal.ReadProcessedItemAsync( thingID, subreddit );
        }
    }
}
