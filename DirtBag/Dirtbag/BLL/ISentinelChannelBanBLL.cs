using System.Collections.Generic;
using System.Threading.Tasks;
using Dirtbag.Models;

namespace Dirtbag.BLL {
    public interface ISentinelChannelBanBLL {
        Task<IEnumerable<SentinelChannelBan>> CheckSentinelChannelBan( string sub, string thingid );
    }
}