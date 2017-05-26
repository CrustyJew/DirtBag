using System.Collections.Generic;
using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.DAL {
    public interface IUserPostingHistoryDAL {
        Task<IEnumerable<UserPostInfo>> GetUserPostingHistoryAsync( string username );
    }
}