using System.Threading.Tasks;

namespace DirtbagWebservice.BLL {
    public class ProcessedPostBLL : IProcessedPostBLL
    {
        private DAL.ProcessedItemSQLDAL dal;
        public ProcessedPostBLL(DAL.ProcessedItemSQLDAL ppDAL ) {
            dal = ppDAL;
        }

        public Task<Models.ProcessedItem> ReadProcessedPost(string thingID, string subreddit) {
            return dal.ReadProcessedItemAsync( thingID, subreddit );
        }
    }
}
