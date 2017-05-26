using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.BLL {
    public interface IAnalyzePostBLL {
        Task<AnalysisResults> AnalyzePost( string subreddit, AnalysisRequest request );
    }
}