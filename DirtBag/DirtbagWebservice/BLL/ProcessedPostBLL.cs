using System.Threading.Tasks;

namespace DirtBagWebservice.BLL {
    public class ProcessedPostBLL {
        private DAL.ProcessedItemSQLDAL dal;
        public ProcessedPostBLL(DAL.ProcessedItemSQLDAL ppDAL ) {
            dal = ppDAL;
        }

        public Task<Models.ProcessedItem> ReadProcessedPost(string thingID, string subreddit) {
            return dal.ReadProcessedItemAsync( thingID, subreddit );
        }
    }
}
