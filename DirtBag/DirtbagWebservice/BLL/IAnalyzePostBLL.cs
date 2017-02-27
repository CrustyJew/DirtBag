using System.Threading.Tasks;
using DirtBagWebservice.Models;

namespace DirtBagWebservice.BLL {
    public interface IAnalyzePostBLL {
        Task<AnalysisResults> AnalyzePost( string subreddit, AnalysisRequest request );
    }
}