using System.Collections.Generic;
using System.Threading.Tasks;
using RedditSharp.Things;

namespace DirtBag.Modules {
	interface IModule {
		IModuleSettings Settings { get; set; }
		Task<Dictionary<string, int>> Analyze( List<Post> posts );
	}
}