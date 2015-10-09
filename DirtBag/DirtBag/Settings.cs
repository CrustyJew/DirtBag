using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace DirtBag {
	[Serializable]
	public class Settings {
		[JsonProperty]
		public static double Version { get; set; }
		[JsonProperty]
		public static List<string> Modules{ get; set; }

		public void Initialize(RedditSharp.WebAgent agent) {

		}

	}
}
