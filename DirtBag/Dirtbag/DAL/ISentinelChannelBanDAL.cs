using System.Collections.Generic;
using System.Threading.Tasks;
using Dirtbag.Models;

namespace Dirtbag.DAL {
    public interface ISentinelChannelBanDAL {
        Task<IEnumerable<SentinelChannelBan>> GetSentinelChannelBan( string sub, string thingid );
    }
}