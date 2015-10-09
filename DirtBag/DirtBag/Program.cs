using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedditSharp;
using System.Configuration;
using System.Threading;

namespace DirtBag {
	class Program {
		public static WebAgent Agent { get; set; }
		public static Reddit Client { get; set; }
		public static RedditAuth Auth { get; set; }
		public static BotSettings Settings { get; set; }

		private static ManualResetEvent waitHandle = new ManualResetEvent( false );
		public const double VersionNumber = 1.0;
		static void Main( string[] args ) {
			//Instantiate and throw away a Reddit instance so the static constructor won't interfere with the WebAgent later.
			new Reddit();

			Initialize();

			waitHandle.WaitOne(); //Go the fuck to sleep
			
		}

		public static void Initialize() {
			string uAgent = ConfigurationManager.AppSettings["UserAgentString"] ;
			string sub = ConfigurationManager.AppSettings["Subreddit"];
			if ( string.IsNullOrEmpty( uAgent ) ) throw new Exception( "Provide setting 'UserAgentString' in AppConfig to avoid Reddit throttling!" );
			if ( string.IsNullOrEmpty( sub ) ) throw new Exception( "Provide setting 'Subreddit' in AppConfig" );

			Agent = new WebAgent();
			WebAgent.EnableRateLimit = true;
			WebAgent.RateLimit = WebAgent.RateLimitMode.Burst;
			WebAgent.RootDomain = "oauth.reddit.com";
           			WebAgent.UserAgent = uAgent;
			Auth = new RedditAuth();
			Auth.Login();

			Client = new Reddit( Auth.AccessToken );

			Settings = new BotSettings();
			Settings.OnSettingsModified += Settings_OnSettingsModified;
			Settings.Subreddit = "GoAwayNoOneLikesYou";
            Settings.Initialize( Client );
		}

		private static void Settings_OnSettingsModified( object sender, EventArgs e ) {
			System.Diagnostics.Debug.WriteLine( "Received settings modified event" );
		}
	}
}
