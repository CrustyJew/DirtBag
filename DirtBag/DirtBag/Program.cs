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

namespace DirtBag {
    class Program {
        public static RedditWebAgent Agent { get; set; }
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
        static void Main(string[] args)
        {
            ActiveModules = new List<IModule>();
            //Instantiate and throw away a Reddit instance so the static constructor won't interfere with the WebAgent later.
            new Reddit();
            var conn = new DirtBagConnection();
            var sub = ConfigurationManager.AppSettings["Subreddit"];
            conn.InitializeConnection(new[] { sub });
            Initialize();

            WaitHandle.WaitOne(); //Go the fuck to sleep

        }

        public static void Initialize() {
            var uAgent = ConfigurationManager.AppSettings["UserAgentString"];
            var sub = ConfigurationManager.AppSettings["Subreddit"];
            if ( string.IsNullOrEmpty( uAgent ) ) throw new Exception( "Provide setting 'UserAgentString' in AppConfig to avoid Reddit throttling!" );
            if ( string.IsNullOrEmpty( sub ) ) throw new Exception( "Provide setting 'Subreddit' in AppConfig" );
            Subreddit = sub;
            Agent = new RedditWebAgent();
            RedditWebAgent.EnableRateLimit = true;
            RedditWebAgent.RateLimit = RedditWebAgent.RateLimitMode.Burst;
            RedditWebAgent.RootDomain = "oauth.reddit.com";
            RedditWebAgent.UserAgent = uAgent;
            RedditWebAgent.Protocol = "https";
            Auth = new RedditAuth( Agent );

            Auth.Login();
            Agent.AccessToken = Auth.AccessToken;
            BurstDebug = new Timer( CheckBurstStats, Agent, 0, 20000 );
            Client = new Reddit( Agent, true );

            Settings = new BotSettings();
            Settings.OnSettingsModified += Settings_OnSettingsModified;
            Settings.Subreddit = Subreddit;
            Settings.Initialize( Client );

        }

        private static void Settings_OnSettingsModified( object sender, EventArgs e ) {
            Console.WriteLine( "Received settings modified event" );
            StopTimer();
            LoadModules();
            StartTimer();
        }

        private static void StartTimer() {
            TheKeeper = new Timer( ProcessPosts, null, 0, Settings.RunEveryXMinutes * 60 * 1000 );
            TheWatcher = new Timer( ProcessMessages, null, 0, Settings.RunEveryXMinutes * 30 * 1000 ); //cheat a bit
        }

        private static void StopTimer() {
            TheKeeper?.Dispose();
            TheWatcher?.Dispose();
        }
        private static void CheckBurstStats( object s ) {
            var agent = (RedditWebAgent) s;
            Console.WriteLine("Last Request: {0}\r\nBurst Start: {1}\r\nRequests this Burst: {2}", agent.LastRequest, agent.BurstStart, agent.RequestsThisBurst);
        }
        private static async void ProcessPosts( object s ) {
            var sub = Client.GetSubreddit( Subreddit );

            var newPosts = new List<Post>();
            var hotPosts = new List<Post>();
            var risingPosts = new List<Post>();

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
            var postComparer = new PostIdEqualityComparer();
            var allPosts = new HashSet<Post>( postComparer );
            allPosts.UnionWith( newPosts );
            allPosts.UnionWith( hotPosts );
            allPosts.UnionWith( risingPosts );
            //Get stats on already processed posts (This could be pulled from mod log at some point if ever desired / found to be more useful)
            var alreadyProcessed = ProcessedPost.CheckProcessed( allPosts.Select( p => p.Id ).ToList() );
            var removedPreviously = new List<string>();
            //select posts that have already been removed once and add them to list
            removedPreviously.AddRange( alreadyProcessed.Where( p => p.Action.ToLower() == "remove" ).Select( p => p.PostID ).ToList() );
            //remove posts from processing that have been removed before. Don't want to override manual mod actions
            newPosts.RemoveAll( p => removedPreviously.Contains( p.Id ) );
            risingPosts.RemoveAll( p => removedPreviously.Contains( p.Id ) );
            hotPosts.RemoveAll( p => removedPreviously.Contains( p.Id ) );

            var reportedPreviously = new List<string>();
            //select posts that have already been removed once and add them to list
            reportedPreviously.AddRange( alreadyProcessed.Where( p => p.Action.ToLower() == "report" ).Select( p => p.PostID ).ToList() );


            var postTasks = new List<Task<Dictionary<string, PostAnalysisResults>>>();


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
                postTasks.Add( module.Analyze( posts.ToList() ) );
            }


            var results = new Dictionary<string, PostAnalysisResults>();

            while ( postTasks.Count > 0 ) {
                var finishedTask = await Task.WhenAny( postTasks );
                postTasks.Remove( finishedTask );
                var result = await finishedTask;
                foreach ( var key in result.Keys ) {
                    if ( results.Keys.Contains( key ) ) {
                        results[key].Scores.AddRange( result[key].Scores );
                    }
                    else {
                        results.Add( key, result[key] );
                    }
                }
            }

            foreach ( var result in results ) {
                var resultVal = result.Value;

                if ( resultVal.TotalScore >= Settings.RemoveScoreThreshold && Settings.RemoveScoreThreshold > 0 ) {
                    resultVal.Post.Remove();
                    try {
                        ProcessedPost.SaveProcessedPost( Settings.Subreddit, resultVal.Post.Id, "Remove" ); //change to enum at some point
                    }
                    catch ( Exception ex ) {
                        Console.WriteLine("Error saving post as processed. Messaage : {0}", "\r\n Inner Exception : " + ex.InnerException.Message);
                    }
                }
                else if ( resultVal.TotalScore >= Settings.ReportScoreThreshold && Settings.ReportScoreThreshold > 0 ) {
                    if (reportedPreviously.Contains(resultVal.Post.Id)) continue;
                    resultVal.Post.Report( VotableThing.ReportType.Other, resultVal.ReportReason );
                    try {
                        ProcessedPost.SaveProcessedPost( Settings.Subreddit, resultVal.Post.Id, "Report" ); //change to enum at some point
                    }
                    catch ( Exception ex ) {
                        Console.WriteLine("Error saving post as processed. Messaage : {0}", "\r\n Inner Exception : " + ex.InnerException.Message);
                    }
                }

            }

            Console.WriteLine("Successfully processed {0} posts. Ignored {1} posts that had been removed already.", results.Keys.Count, removedPreviously.Count);
        }
        internal static async Task<PostAnalysisResults> AnalyzePost( Post post ) {
            var postTasks = ActiveModules.Select(module => module.Analyze(new List<Post> {post})).ToList();
            var results = new PostAnalysisResults( post );

            while ( postTasks.Count > 0 ) {
                var finishedTask = await Task.WhenAny( postTasks );
                postTasks.Remove( finishedTask );
                var result = await finishedTask;
                foreach ( var key in result.Keys ) {
                    results.Scores.AddRange( result[key].Scores );
                }
            }
            return results;
        }
        private static async void ProcessMessages( object s ) {
            var messages = Client.User.UnreadMessages;
            var mods = new List<string>();
            mods.AddRange( Client.GetSubreddit( Subreddit ).Moderators.Select( m => m.Name.ToLower() ).ToList() ); //TODO when enabling multiple subs, fix this

            foreach (var message in messages.Where(unread => unread.Kind == "t4").Cast<PrivateMessage>())
            {
                message.SetAsRead();
                if (message.Subject.ToLower() != "validate" && message.Subject.ToLower() != "check" &&
                    message.Subject.ToLower() != "analyze") continue;
                Post post;
                try {
                    post = Client.GetPost( new Uri( message.Body ) );
                }
                catch {
                    message.Reply( "That URL made me throw up in my mouth a little. Try again!" );
                    continue;
                }
                if ( post.SubredditName.ToLower() != Subreddit.ToLower() ) { //TODO when enabling multiple subreddits, this needs tweaked!
                    message.Reply($"I don't have any rules for {post.SubredditName}.");
                }
                else if ( !mods.Contains( message.Author.ToLower() ) ) {
                    message.Reply($"You aren't a mod of {post.SubredditName}! What are you doing here? Go on! GIT!");
                }
                else if ( post.AuthorName == "[deleted]" ) {
                    message.Reply( "The OP deleted the post so I can't check it. Sorry (read in Canadian accent)!" );
                }
                else {
                    //omg finally analyze the damn thing
                    var result = await AnalyzePost( post );
                    var reply = new StringBuilder();
                    reply.AppendLine(
                        $"Analysis results for \"[{post.Title}]({post.Permalink})\" submitted by /u/{post.AuthorName} to /r/{post.SubredditName}");
                    reply.AppendLine();
                    var action = "None";
                    if ( Settings.RemoveScoreThreshold > 0 && result.TotalScore > Settings.RemoveScoreThreshold ) action = "Remove";
                    if ( Settings.ReportScoreThreshold > 0 && result.TotalScore > Settings.ReportScoreThreshold ) action = "Report";
                    reply.AppendLine($"##Action Taken: {action} with a score of {result.TotalScore}");
                    reply.AppendLine();
                    reply.AppendLine(
                        $"**/r/{post.SubredditName}'s thresholds** --- Remove : **{(Settings.RemoveScoreThreshold > 0 ? Settings.RemoveScoreThreshold.ToString() : "Disabled")}** , Report : **{(Settings.ReportScoreThreshold > 0 ? Settings.ReportScoreThreshold.ToString() : "Disabled")}**");
                    reply.AppendLine();
                    reply.AppendLine( "Module| Score |Reason" );
                    reply.AppendLine( ":--|:--:|:--" );
                    foreach ( var score in result.Scores ) {
                        reply.AppendLine($"{score.ModuleName}|{score.Score}|{score.Reason}");
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
            /*** End Load Modules ***/
        }
    }
}
