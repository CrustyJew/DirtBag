using System.Threading.Tasks;
using DirtbagWebservice.Models;

namespace DirtbagWebservice.BLL {
    public interface IAnalyzeMediaBLL {
        Task<Models.ProcessedItem> UpdateAnalysisAsync( string subreddit, string thingID, string mediaID, Models.VideoProvider mediaPlatform, string updateBy );
        Task<Models.AnalysisResults> AnalyzeMedia( string subreddit, Models.AnalysisRequest request, bool actOnInfo);
        Task<Models.AnalysisResults> AnalyzeMedia( Models.SubredditSettings settings, Models.AnalysisRequest request, bool actOnInfo );
    }
}