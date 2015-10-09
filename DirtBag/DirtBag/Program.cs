using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedditSharp;
using System.Configuration;

namespace DirtBag {
	class Program {
		public static WebAgent Agent { get; set; }
		public static Reddit Client { get; set; }
		static void Main( string[] args ) {
			//Instantiate and throw away a Reddit instance so the static constructor won't interfere with the WebAgent later.
			new Reddit();

			Initialize();

			System.Threading.Thread.Sleep( System.Threading.Timeout.Infinite ); //Go the fuck to sleep
			
		}

		public static void Initialize() {
			Agent = new WebAgent();
			WebAgent.EnableRateLimit = true;
			WebAgent.RateLimit = WebAgent.RateLimitMode.Burst;
			WebAgent.RootDomain = "oauth.reddit.com";
            if ( string.IsNullOrEmpty( ConfigurationManager.AppSettings["UserAgentString"] ) ) throw new Exception( "Provide setting 'UserAgentString' in AppConfig to avoid Reddit throttling!" );
			WebAgent.UserAgent = ConfigurationManager.AppSettings["UserAgentString"];
			
		}
	}
}
