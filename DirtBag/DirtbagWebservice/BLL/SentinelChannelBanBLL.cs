using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtbagWebservice.BLL
{
    public class SentinelChannelBanBLL : ISentinelChannelBanBLL {
        private DAL.ISentinelChannelBanDAL dal;
        public SentinelChannelBanBLL( DAL.ISentinelChannelBanDAL sentinelDAL ) {
            dal = sentinelDAL;
        }
        public Task<IEnumerable<Models.SentinelChannelBan>> CheckSentinelChannelBan(string sub, string thingid) {
            return dal.GetSentinelChannelBan(sub, thingid);
        }
    }
}
