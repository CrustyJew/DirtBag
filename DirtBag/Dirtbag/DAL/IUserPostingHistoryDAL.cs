using System.Collections.Generic;
using System.Threading.Tasks;
using Dirtbag.Models;

namespace Dirtbag.DAL {
    public interface IUserPostingHistoryDAL {
        Task<IEnumerable<UserPostInfo>> GetUserPostingHistoryAsync( string username );
    }
}