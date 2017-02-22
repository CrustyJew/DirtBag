using System.Collections.Generic;
using System.Threading.Tasks;
using DirtBagWebservice.Models;

namespace DirtBagWebservice.DAL {
    public interface IUserPostingHistoryDAL {
        Task<IEnumerable<UserPostInfo>> GetUserPostingHistoryAsync( string username );
    }
}