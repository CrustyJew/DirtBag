using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.BLL {
    public class ProcessedPostBLL {
        private DAL.ProcessedPostSQLDAL dal;
        public ProcessedPostBLL(DAL.ProcessedPostSQLDAL ppDAL ) {
            dal = ppDAL;
        }

        public Task<Models.ProcessedItem> ReadProcessedPost(string thingID, string subreddit) {
            return dal.ReadProcessedItem( thingID, subreddit );
        }
    }
}
