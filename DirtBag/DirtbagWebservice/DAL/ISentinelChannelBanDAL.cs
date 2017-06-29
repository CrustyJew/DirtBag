using System.Collections.Generic;
using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.DAL {
    public interface ISentinelChannelBanDAL {
        Task<IEnumerable<SentinelChannelBan>> GetSentinelChannelBan( string sub, string thingid );
    }
}