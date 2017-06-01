using System.Threading.Tasks;

namespace DirtbagWebservice.BLL {
    public class ProcessedItemBLL : IProcessedItemBLL
    {
        private DAL.ProcessedItemSQLDAL dal;
        public ProcessedItemBLL(DAL.ProcessedItemSQLDAL ppDAL ) {
            dal = ppDAL;
        }

        public Task<IEnumerable<Models.ProcessedItem>> ReadProcessedPost(string thingID, string subreddit) {
            return dal.ReadProcessedItemAsync( thingID, subreddit );
        }
    }
}
