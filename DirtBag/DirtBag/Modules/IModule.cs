using System.Collections.Generic;
using System.Threading.Tasks;
using RedditSharp.Things;

namespace DirtBag.Modules {
	interface IModule {
		string ModuleName { get; }
        bool MultiScan { get; }
		IModuleSettings Settings { get; set; }
		Task<Dictionary<string, PostAnalysisResults>> Analyze( List<Post> posts );
	}
}