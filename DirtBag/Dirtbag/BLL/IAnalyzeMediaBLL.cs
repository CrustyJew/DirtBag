using System.Threading.Tasks;
using Dirtbag.Models;
using System.Collections.Generic;

namespace Dirtbag.BLL {
    public interface IAnalyzeMediaBLL {
        Task UpdateAnalysisAsync( IEnumerable<string> thingIDs, string subreddit, string updateBy );
        Task<Models.AnalysisResults> AnalyzeMedia( string subreddit, Models.AnalysisRequest request, bool actOnInfo);
        Task<Models.AnalysisResults> AnalyzeMedia( Models.SubredditSettings settings, Models.AnalysisRequest request, bool actOnInfo );
    }
}