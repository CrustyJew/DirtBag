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
using Microsoft.Practices.Unity;

namespace DirtBag {
    class Program : RoleEntryPoint {
        private static IDisposable _app;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent( false );
        private static List<string> endpts;

        public static BotWebAgent Agent { get; set; }
        public static Reddit Client { get; set; }
        public static RedditAuth Auth { get; set; }
        public static Models.SubredditSettings Settings { get; set; }
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
            Task.Run(()=>Initialize());
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
                var _container = UnityHelpers.GetConfiguredContainer();
                var start = _container.Resolve<Startup>();
                _app = WebApp.Start( opts,start.Configuration );
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

                await Task.Run( () => Main( endpts.ToArray() ) );
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
            var wikiSettings = new DAL.SubredditSettingsWikiDAL( Client );
            var processedItemDAL = new DAL.ProcessedItemSQLDAL( DirtBagConnection.GetConn() );
            var bot = new DirtbagBot( sub, Agent, Client, wikiSettings, processedItemDAL );
            Task.Run( () => bot.StartBot() );

            var messageListener = new DirtbagMessageListener( Client, processedItemDAL );
            messageListener.StartListener();
            
        }


        private static void CheckBurstStats( object s ) {
            var agent = (BotWebAgent) s;
            Console.WriteLine( "Last Request: {0}\r\nBurst Start: {1}\r\nRequests this Burst: {2}", agent.LastRequest, agent.BurstStart, agent.RequestsThisBurst );
        }
        
    }
}
