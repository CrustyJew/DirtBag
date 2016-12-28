using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DirtBag.Helpers;
using DirtBag.Logging;
using DirtBag.Modules;
using RedditSharp;
using RedditSharp.Things;
using Microsoft.Owin.Hosting;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;
using System.Net;
using DirtBag.Models;

namespace DirtBag {
    class Program : RoleEntryPoint {
        private static IDisposable _app;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent( false );
        private static List<string> endpts;

        public static BotWebAgent Agent { get; set; }
        public static Reddit Client { get; set; }
        public static RedditAuth Auth { get; set; }
        public static BotSettings Settings { get; set; }
        public static string Subreddit { get; set; }
        public static Timer TheKeeper { get; set; }
        public static Timer TheWatcher { get; set; }
        private static Timer BurstDebug { get; set; }
        public static List<IModule> ActiveModules { get; set; }

        private static readonly ManualResetEvent WaitHandle = new ManualResetEvent( false );
        public const double VersionNumber = 1.0;
        static void Main( string[] args ) {
            ActiveModules = new List<IModule>();
            //Instantiate and throw away a Reddit instance so the static constructor won't interfere with the WebAgent later.
            new Reddit();
            var conn = new DirtBagConnection();
            var sub = ConfigurationManager.AppSettings["Subreddit"];
            conn.InitializeConnection( new[] { sub } );
            Task.Factory.StartNew(Initialize);
            string baseAddresses = System.Configuration.ConfigurationManager.AppSettings["ApiListeningUrls"];
            baseAddresses += "," + string.Join(",",args);
            if ( !string.IsNullOrWhiteSpace( baseAddresses ) ) {
                var opts = new StartOptions();
                foreach ( string address in baseAddresses.Split( ',' ) ) {
                    if ( !string.IsNullOrWhiteSpace( address ) ) {
                        opts.Urls.Add( address );
                    }
                }
                Console.WriteLine( "Starting web app. Listening on: " + String.Join( ", ", opts.Urls ) );
                _app = WebApp.Start<Startup>( opts );
            }

            WaitHandle.WaitOne(); //Go the fuck to sleep



        }
        public override void Run() {
            Trace.TraceInformation( "Dirtbag Worker Role is running" );

            try {
                this.RunAsync( this.cancellationTokenSource.Token ).Wait();
            }
            finally {
                this.runCompleteEvent.Set();
            }
        }
        private async Task RunAsync( CancellationToken cancellationToken ) {
            // TODO: Replace the following with your own logic.
            while ( !cancellationToken.IsCancellationRequested ) {
                Trace.TraceInformation( "Working" );

                await Task.Factory.StartNew( () => Main( endpts.ToArray() ) );
            }
        }
        public override bool OnStart() {

            var endpoints = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints;
            ServicePointManager.DefaultConnectionLimit = 12;
            endpts = new List<string>();
            foreach(var end in endpoints.Values ) {
                if ( end.Protocol.StartsWith( "http" ) ) {
                    endpts.Add( String.Format( "{0}://{1}", end.Protocol, end.IPEndpoint ) );
                }
            }
            Trace.WriteLine( string.Join( ", ", endpts ) );
            return base.OnStart();
        }
        public override void OnStop() {
            if ( _app != null ) {
                _app.Dispose();
            }
            WaitHandle.Close();
            base.OnStop();
        }

        public static void Initialize() {
            var uAgent = ConfigurationManager.AppSettings["UserAgentString"];
            var sub = ConfigurationManager.AppSettings["Subreddit"];
            if ( string.IsNullOrEmpty( uAgent ) ) throw new Exception( "Provide setting 'UserAgentString' in AppConfig to avoid Reddit throttling!" );
            if ( string.IsNullOrEmpty( sub ) ) throw new Exception( "Provide setting 'Subreddit' in AppConfig" );
            Subreddit = sub;
            string uname = ConfigurationManager.AppSettings["BotUsername"];
            string pass = ConfigurationManager.AppSettings["BotPassword"];
            string clientId = ConfigurationManager.AppSettings["ClientID"];
            string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            string redirectUri = ConfigurationManager.AppSettings["RedirectURI"];
            if ( string.IsNullOrEmpty( uname ) ) throw new Exception( "Missing 'BotUsername' in config" );
            if ( string.IsNullOrEmpty( pass ) ) throw new Exception( "Missing 'BotPassword' in config" );
            if ( string.IsNullOrEmpty( clientId ) ) throw new Exception( "Missing 'ClientID' in config" );
            if ( string.IsNullOrEmpty( clientSecret ) ) throw new Exception( "Missing 'ClientSecret' in config" );
            if ( string.IsNullOrEmpty( redirectUri ) ) throw new Exception( "Missing 'RedirectURI' in config" );
            Agent = new BotWebAgent(uname,pass,clientId,clientSecret,redirectUri);
            BotWebAgent.EnableRateLimit = true;
            BotWebAgent.RateLimit = BotWebAgent.RateLimitMode.Burst;
            BotWebAgent.RootDomain = "oauth.reddit.com";
            BotWebAgent.UserAgent = uAgent;
            BotWebAgent.Protocol = "https";

            
            //BurstDebug = new Timer( CheckBurstStats, Agent, 0, 20000 );
            Client = new Reddit( Agent, true );

            Settings = new BotSettings();
            Settings.OnSettingsModified += Settings_OnSettingsModified;
            Settings.Subreddit = Subreddit;
            Settings.Initialize( Client );

            while ( true ) {
                Task.Factory.StartNew( ProcessMessages ).Wait();
            }
        }

        private static void Settings_OnSettingsModified( object sender, EventArgs e ) {
            Console.WriteLine( "Received settings modified event" );
            StopTimer();
            LoadModules();
            StartTimer();
        }

        private static void StartTimer() {
            TheKeeper = new Timer( ProcessPosts, null, 0, Settings.RunEveryXMinutes * 60 * 1000 );
        }

        private static void StopTimer() {
            TheKeeper?.Dispose();
            TheWatcher?.Dispose();
        }
        private static void CheckBurstStats( object s ) {
            var agent = (BotWebAgent) s;
            Console.WriteLine( "Last Request: {0}\r\nBurst Start: {1}\r\nRequests this Burst: {2}", agent.LastRequest, agent.BurstStart, agent.RequestsThisBurst );
        }
        private static async void ProcessPosts( object s ) {
            var sub = Client.GetSubreddit( Subreddit );
            var ppBLL = new BLL.ProcessedPostBLL(new DAL.ProcessedItemSQLDAL(new SqlConnection ))
            var newPosts = new List<Post>();
            var hotPosts = new List<Post>();
            var risingPosts = new List<Post>();

            //avoid getting unnecessary posts to keep requests lower
            if ( ActiveModules.Any( m => m.Settings.PostTypes.HasFlag( PostType.New ) ) ) {
                newPosts = sub.New.Take( 100 ).ToList();
            }
            if ( ActiveModules.Any( m => m.Settings.PostTypes.HasFlag( PostType.Hot ) ) ) {
                hotPosts = sub.Hot.Take( 50 ).ToList();
            }
            if ( ActiveModules.Any( m => m.Settings.PostTypes.HasFlag( PostType.Rising ) ) ) {
                risingPosts = sub.Rising.Take( 50 ).ToList();
            }
            var postComparer = new PostIdEqualityComparer();
            var allPosts = new HashSet<Post>( postComparer );
            allPosts.UnionWith( newPosts );
            allPosts.UnionWith( hotPosts );
            allPosts.UnionWith( risingPosts );
            //Get stats on already processed posts (This could be pulled from mod log at some point if ever desired / found to be more useful)
            var alreadyProcessed = ProcessedPost.GetProcessed( allPosts.Select( p => p.Id ).ToList() );
            var removedPreviously = new List<ProcessedPost>();
            //select posts that have already been removed once and add them to list
            removedPreviously.AddRange( alreadyProcessed.Where( p => p.Action.ToLower() == "remove" ) );

            var reportedPreviously = new List<ProcessedPost>();
            //select posts that have already been removed once and add them to list
            reportedPreviously.AddRange( alreadyProcessed.Where( p => p.Action.ToLower() == "report" ) );


            var postTasks = new List<Task<Dictionary<string, AnalysisDetails>>>();


            foreach ( var module in ActiveModules ) {
                //hashset to prevent duplicates being passed.
                var posts = new HashSet<Post>( postComparer );
                if ( module.Settings.PostTypes.HasFlag( PostType.New ) ) {
                    posts.UnionWith( newPosts );
                }
                if ( module.Settings.PostTypes.HasFlag( PostType.Hot ) ) {
                    posts.UnionWith( hotPosts );
                }
                if ( module.Settings.PostTypes.HasFlag( PostType.Rising ) ) {
                    posts.UnionWith( risingPosts );
                }
                List<Post> postsList = new List<Post>();
                if ( !module.MultiScan ) {
                    //only add unseen posts
                    postsList.AddRange( posts.Where( ph => alreadyProcessed.Count( ap => ap.PostID == ph.Id && ap.SeenByModules.HasFlag( module.ModuleEnum ) ) == 0 ) );
                }
                else {
                    postsList = posts.ToList();
                }
                if ( postsList.Count > 0 ) postTasks.Add( Task.Run( () => module.Analyze( postsList ) ) );
            }

            var results = new Dictionary<string, AnalysisDetails>();
            while ( postTasks.Count > 0 ) {
                var finishedTask = await Task.WhenAny( postTasks );
                postTasks.Remove( finishedTask );
                var result = await finishedTask;
                foreach ( var key in result.Keys ) {
                    if ( results.Keys.Contains( key ) ) {
                        results[key].Scores.AddRange( result[key].Scores );
                        results[key].AnalyzingModule = results[key].AnalyzingModule | result[key].AnalyzingModule;
                    }
                    else {
                        results.Add( key, result[key] );
                    }
                }
            }
            int ignoredCounter = 0, reportedCounter = 0, removedCounter = 0;

            foreach ( var result in results ) {
                var combinedAnalysis = result.Value;
                string action = "None"; //change to Enum at some point
                bool unseen = false;
                ProcessedPost original = alreadyProcessed.SingleOrDefault( p => p.PostID == combinedAnalysis.Post.Id );
                if ( original == null ) {
                    original = new ProcessedPost( Settings.Subreddit, combinedAnalysis.Post.Id, "invalid" );
                    unseen = true;
                }
                else {
                    var prevScores = original.AnalysisResults.Scores.Where( os => combinedAnalysis.Scores.Count( cs => cs.ModuleName == os.ModuleName ) == 0 ).ToList();
                    combinedAnalysis.Scores.AddRange( prevScores );
                    combinedAnalysis.AnalyzingModule = original.SeenByModules | combinedAnalysis.AnalyzingModule;
                }
                if ( combinedAnalysis.TotalScore >= Settings.RemoveScoreThreshold && Settings.RemoveScoreThreshold > 0 ) {
                    ProcessedPost removed = removedPreviously.SingleOrDefault( p => p.PostID == combinedAnalysis.Post.Id );
                    if ( removed == null || removed.AnalysisResults.TotalScore < combinedAnalysis.TotalScore ) {
                        //only remove the post if it wasn't previously removed by the bot, OR if the score has increased
                        combinedAnalysis.Post.Remove();
                        if ( combinedAnalysis.HasFlair ) {
                            combinedAnalysis.Post.SetFlair( combinedAnalysis.FlairText, combinedAnalysis.FlairClass );
                        }
                        removedCounter++;
                    }
                    else {
                        ignoredCounter++;
                    }
                    action = "Remove";
                }
                else if ( combinedAnalysis.TotalScore >= Settings.ReportScoreThreshold && Settings.ReportScoreThreshold > 0 ) {
                    if ( reportedPreviously.Count( p => p.PostID == combinedAnalysis.Post.Id ) == 0 ) {
                        //can't change report text or report an item again. Thanks Obama... err... Reddit...
                        combinedAnalysis.Post.Report( VotableThing.ReportType.Other, combinedAnalysis.ReportReason );
                        reportedCounter++;
                    }
                    action = "Report";
                }
                if ( combinedAnalysis.TotalScore != original.AnalysisResults.TotalScore || action != original.Action || original.SeenByModules != combinedAnalysis.AnalyzingModule ) {
                    if ( combinedAnalysis.TotalScore > 0 ) {
                        original.AnalysisResults = combinedAnalysis;
                    }
                    else original.AnalysisResults = null;

                    original.SeenByModules = original.SeenByModules | combinedAnalysis.AnalyzingModule;
                    original.Action = action;
                    //processed post needs updated in
                    if ( unseen ) {
                        try {
                            //ProcessedPost.AddProcessedPost( original ); //TODO
                        }
                        catch ( Exception ex ) {
                            Console.WriteLine( "Error adding new post as processed. Messaage : {0}", "\r\n Inner Exception : " + ex.InnerException?.Message );
                        }
                    }
                    else {
                        try {
                            //ProcessedPost.UpdateProcessedPost( original ); //TODO
                        }
                        catch ( Exception ex ) {
                            Console.WriteLine( "Error updating processed post. Messaage : {0}", "\r\n Inner Exception : " + ( ex.InnerException != null ? ex.InnerException.Message : "null" ) );
                        }
                    }
                }
            }

            Console.WriteLine( $"Successfully processed {results.Keys.Count} posts.\r\nIgnored posts: {ignoredCounter}\r\nReported Posts: {reportedCounter}\r\nRemoved Posts: {removedCounter}" );

        }
        internal static async Task<AnalysisDetails> AnalyzePost( Post post ) {
            var postTasks = ActiveModules.Select( module => module.Analyze( new List<Post> { post } ) ).ToList();
            var results = new AnalysisDetails( post, Modules.Modules.None );

            while ( postTasks.Count > 0 ) {
                var finishedTask = await Task.WhenAny( postTasks );
                postTasks.Remove( finishedTask );
                var result = await finishedTask;
                foreach ( var key in result.Keys ) {
                    results.Scores.AddRange( result[key].Scores );
                    results.AnalyzingModule = results.AnalyzingModule | result[key].AnalyzingModule;
                }
            }
            return results;
        }
        private static async void ProcessMessages() {
            var messages = Client.User.UnreadMessages.GetListingStream();
            
            var mods = new List<string>();
            mods.AddRange( Client.GetSubreddit( Subreddit ).Moderators.Select( m => m.Name.ToLower() ).ToList() ); //TODO when enabling multiple subs, fix this

            foreach ( var message in messages.Where( unread => unread.Kind == "t4" ).Cast<PrivateMessage>() ) {
                message.SetAsRead();
                string subject = message.Subject.ToLower();
                List<string> args = subject.Split( '-' ).Select( p => p.Trim() ).ToList();

                bool force = args.Count > 1 && args.Contains( "force" );
                if ( ! subject.Contains("validate") && !subject.Contains("check") &&
                    !subject.Contains("analyze" ) && !subject.Contains("test") && !subject.Contains("verify") ) {
                    message.Reply( "Whatchu talkin bout Willis" );
                    continue;
                }
                Post post;
                try {
                    post = Client.GetPost( new Uri( message.Body ) );
                }
                catch {
                    message.Reply( "That URL made me throw up in my mouth a little. Try again!" );
                    continue;
                }
                if ( post.SubredditName.ToLower() != Subreddit.ToLower() ) { //TODO when enabling multiple subreddits, this needs tweaked!
                    message.Reply( $"I don't have any rules for {post.SubredditName}." );
                }
                else if ( !mods.Contains( message.Author.ToLower() ) ) {
                    message.Reply( $"You aren't a mod of {post.SubredditName}! What are you doing here? Go on! GIT!" );
                }
                else {
                    //omg finally analyze the damn thing
                    PostAnalysisResults result;
                    var original = ProcessedPost.GetProcessed( new List<string>() { post.Id } ).SingleOrDefault();
                    if ( (int) original.SeenByModules == ActiveModules.Sum( a => (int) a.ModuleEnum ) && !force && original.AnalysisResults != null ) {
                        result = original.AnalysisResults;
                    }
                    else if ( post.AuthorName == "[deleted]" ) {
                        message.Reply( "The OP deleted the post, and I don't have it cached so I can't check it. Sorry (read in Canadian accent)!" );
                        continue;
                    }
                    else {
                        result = await AnalyzePost( post );
                    }
                    var reply = new StringBuilder();
                    reply.AppendLine(
                        $"Analysis results for \"[{post.Title}]({post.Permalink})\" submitted by /u/{post.AuthorName} to /r/{post.SubredditName}" );
                    reply.AppendLine();
                    var action = "None";
                    if ( Settings.RemoveScoreThreshold > 0 && result.TotalScore >= Settings.RemoveScoreThreshold ) action = "Remove";
                    else if ( Settings.ReportScoreThreshold > 0 && result.TotalScore >= Settings.ReportScoreThreshold ) action = "Report";
                    reply.AppendLine($"##Action Taken: {action} with a score of {result.TotalScore}");
                    reply.AppendLine();
                    reply.AppendLine(
                        $"**/r/{post.SubredditName}'s thresholds** --- Remove : **{( Settings.RemoveScoreThreshold > 0 ? Settings.RemoveScoreThreshold.ToString() : "Disabled" )}** , Report : **{( Settings.ReportScoreThreshold > 0 ? Settings.ReportScoreThreshold.ToString() : "Disabled" )}**" );
                    reply.AppendLine();
                    reply.AppendLine( "Module| Score |Reason" );
                    reply.AppendLine( ":--|:--:|:--" );
                    foreach ( var score in result.Scores ) {
                        reply.AppendLine( $"{score.ModuleName}|{score.Score}|{score.Reason}" );
                    }
                    message.Reply( reply.ToString() );

                }
            }
        }
        private static void LoadModules() {
            ActiveModules.Clear();
            /*** Load Modules ***/
            if ( Settings.LicensingSmasher.Enabled ) ActiveModules.Add( new LicensingSmasher( Settings.LicensingSmasher, Client, Subreddit ) );
            if ( Settings.YouTubeSpamDetector.Enabled ) ActiveModules.Add( new YouTubeSpamDetector( Settings.YouTubeSpamDetector, Client, Subreddit ) );
            if ( Settings.UserStalker.Enabled ) ActiveModules.Add( new UserStalker( Settings.UserStalker, Client, Subreddit ) );
            if ( Settings.SelfPromotionCombustor.Enabled ) ActiveModules.Add( new SelfPromotionCombustor( Settings.SelfPromotionCombustor, Client ) );
            if ( Settings.HighTechBanHammer.Enabled ) ActiveModules.Add( new HighTechBanHammer( Settings.HighTechBanHammer, Client.GetSubreddit(Subreddit) ) );
            /*** End Load Modules ***/
        }
    }
}
