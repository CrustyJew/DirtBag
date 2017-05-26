using System.Threading.Tasks;

namespace DirtbagWebservice.BLL {
    public class ProcessedItemBLL : IProcessedItemBLL
    {
        private DAL.ProcessedItemSQLDAL dal;
        public ProcessedItemBLL(DAL.ProcessedItemSQLDAL ppDAL ) {
            dal = ppDAL;
        }

        public Task<Models.ProcessedItem> ReadProcessedPost(string thingID, string subreddit, string mediaID, Models.VideoProvider mediaPlatform) {
            return dal.ReadProcessedItemAsync( thingID, subreddit, mediaID, mediaPlatform );
        }
    }
}
