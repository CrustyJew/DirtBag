using System.Collections.Generic;
using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.BLL {
    public interface ISentinelChannelBanBLL {
        Task<IEnumerable<SentinelChannelBan>> CheckSentinelChannelBan( string sub, string thingid );
    }
}