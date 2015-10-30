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
		public static string Subreddit { get; set; }
		public static Timer TheKeeper { get; set; }

		public static List<Modules.IModule> ActiveModules { get; set; }

		private static ManualResetEvent waitHandle = new ManualResetEvent( false );
		public const double VersionNumber = 1.0;
		static void Main( string[] args ) {
			ActiveModules = new List<DirtBag.Modules.IModule>();
			//Instantiate and throw away a Reddit instance so the static constructor won't interfere with the WebAgent later.
			new Reddit();

			Initialize();

			waitHandle.WaitOne(); //Go the fuck to sleep

		}

		public static void Initialize() {
			string uAgent = ConfigurationManager.AppSettings["UserAgentString"];
			string sub = ConfigurationManager.AppSettings["Subreddit"];
			if ( string.IsNullOrEmpty( uAgent ) ) throw new Exception( "Provide setting 'UserAgentString' in AppConfig to avoid Reddit throttling!" );
			if ( string.IsNullOrEmpty( sub ) ) throw new Exception( "Provide setting 'Subreddit' in AppConfig" );
			Subreddit = sub;
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
			Settings.Subreddit = Subreddit;
			Settings.Initialize( Client );

		}

		private static void Settings_OnSettingsModified( object sender, EventArgs e ) {
			System.Diagnostics.Debug.WriteLine( "Received settings modified event" );
			StopTimer();
			LoadModules();
			StartTimer();
		}

		private static void StartTimer() {
			TheKeeper = new Timer( ProcessPosts, null, 0, Settings.RunEveryXMinutes * 60 * 1000 );
		}

		private static void StopTimer() {
			if ( TheKeeper != null ) {
				TheKeeper.Dispose();
			}
		}

		private static async void ProcessPosts(object s ) {
			RedditSharp.Things.Subreddit sub = Client.GetSubreddit( Subreddit );

			List<RedditSharp.Things.Post> newPosts = new List<RedditSharp.Things.Post>();
			List<RedditSharp.Things.Post> hotPosts = new List<RedditSharp.Things.Post>(); 
			List<RedditSharp.Things.Post> risingPosts = new List<RedditSharp.Things.Post>(); 

			//avoid getting unnecessary posts to keep requests lower
			if ( ActiveModules.Any( m => m.Settings.PostTypes.HasFlag( PostType.New ) ) ) {
				newPosts = sub.New.Take( 50 ).ToList();
			}
			if ( ActiveModules.Any( m => m.Settings.PostTypes.HasFlag( PostType.Hot ) ) ) {
				hotPosts = sub.Hot.Take( 50 ).ToList();
			}
			if ( ActiveModules.Any( m => m.Settings.PostTypes.HasFlag( PostType.Rising ) ) ) {
				risingPosts = sub.Rising.Take( 50 ).ToList();
			}

			List<Task<Dictionary<string, Modules.PostAnalysisResults>>> postTasks = new List<Task<Dictionary<string, Modules.PostAnalysisResults>>>();

			var postComparer = new Helpers.PostIdEqualityComparer();
            foreach (var module in ActiveModules ) {
				//hashset to prevent duplicates being passed.
				HashSet<RedditSharp.Things.Post> posts = new HashSet<RedditSharp.Things.Post>( postComparer );
				if ( module.Settings.PostTypes.HasFlag( PostType.New ) ) {
					posts.UnionWith( newPosts );
				}
				if ( module.Settings.PostTypes.HasFlag( PostType.Hot ) ) {
					posts.UnionWith( hotPosts );
				}
				if ( module.Settings.PostTypes.HasFlag( PostType.Rising ) ) {
					posts.UnionWith( risingPosts );
				}
				postTasks.Add( module.Analyze( posts.ToList() ) );
			}


			Dictionary<string, Modules.PostAnalysisResults > results = new Dictionary<string, Modules.PostAnalysisResults>();

			while (postTasks.Count > 0 ) {
				var finishedTask = await Task.WhenAny( postTasks );
				postTasks.Remove( finishedTask );
				var result = await finishedTask;
				foreach(string key in result.Keys ) {
					if(results.Keys.Contains( key ) ) {
						results[key].Scores.AddRange(result[key].Scores);
					}
					else {
						results.Add( key, result[key] );
					}
				}
			}

			System.Diagnostics.Debug.WriteLine( String.Format( "Successfully processed {0} posts", results.Keys.Count ) );
		}

		private static void LoadModules() {
			ActiveModules.Clear();
			/*** Load Modules ***/
			if ( Settings.LicensingSmasher.Enabled ) ActiveModules.Add( new Modules.LicensingSmasher( Settings.LicensingSmasher, Client, Subreddit ) );
			/*** End Load Modules ***/
		}
	}
}
