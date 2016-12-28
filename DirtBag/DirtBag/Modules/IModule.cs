using System.Collections.Generic;
using System.Threading.Tasks;
using RedditSharp.Things;

namespace DirtBag.Modules {
    public interface IModule {
        string ModuleName { get; }
        Modules ModuleEnum { get; }
        bool MultiScan { get; }
        Models.IModuleSettings Settings { get; set; }
        Task<Dictionary<string, Models.AnalysisDetails>> Analyze( List<Post> posts );
    }
}