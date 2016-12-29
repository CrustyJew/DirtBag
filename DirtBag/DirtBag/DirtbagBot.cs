using RedditSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DirtBag.Modules;
using DirtBag.Models;
using RedditSharp.Things;
using DirtBag.Helpers;

namespace DirtBag {
    public class DirtbagBot {
        private BotWebAgent agent;
        private Reddit client;
        private DAL.ISubredditSettingsDAL subSettingsDAL;
        private DAL.IProcessedItemDAL processedDAL;
        public Models.SubredditSettings Settings { get; set; }

        private Timer TheKeeper;
        private Timer TheWatcher;
        private Timer BurstDebug;

        public List<IModule> ActiveModules { get; set; }
        public string Subreddit { get; }

        public const double VersionNumber = 2.0;

        public DirtbagBot( string subreddit, BotWebAgent botAgent, Reddit redditClient, DAL.ISubredditSettingsDAL settingsDAL, DAL.IProcessedItemDAL processedItemDAL ) {
            Subreddit = subreddit;
            agent = botAgent;
            client = redditClient;
            subSettingsDAL = settingsDAL;
            processedDAL = processedItemDAL;
            ActiveModules = new List<IModule>();
        }

        public async Task StartBot() {
            Settings = await subSettingsDAL.GetSubredditSettingsAsync( Subreddit );
            LoadModules();
            TheKeeper = new Timer( TimerTask, null, Settings.RunEveryXMinutes * 60 * 1000, Settings.RunEveryXMinutes * 60 * 1000 );
        }

        private void TimerTask( object s ) {
            TheKeeper.Dispose();
            var processed = Task.Run( ProcessPosts );
            processed.Wait();
            var x = processed.Result;
            TheKeeper = new Timer( TimerTask, null, Settings.RunEveryXMinutes * 60 * 1000, Settings.RunEveryXMinutes * 60 * 1000 );
        }

        private async Task ProcessPosts() {
            var sub = client.GetSubreddit( Subreddit );
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
            var alreadyProcessed = await processedDAL.ReadProcessedItemsAsync( allPosts.Select( p => p.Id ).ToList(), Subreddit );
            var removedPreviously = new List<ProcessedItem>();
            //select posts that have already been removed once and add them to list
            removedPreviously.AddRange( alreadyProcessed.Where( p => p.Action.ToLower() == "remove" ) );

            var reportedPreviously = new List<ProcessedItem>();
            //select posts that have already been removed once and add them to list
            reportedPreviously.AddRange( alreadyProcessed.Where( p => p.Action.ToLower() == "report" ) );


            var postTasks = new List<Task<Dictionary<string, AnalysisDetails>>>();


            foreach ( var module in ActiveModules ) {
                //hashset to prevent duplicates being passed.
                var posts = new HashSet<Post>( postComparer );
                if ( module.Settings.PostTypes.HasFlag( PostType.All ) ) {
                    posts = allPosts;
                }
                else {
                    if ( module.Settings.PostTypes.HasFlag( PostType.New ) ) {
                        posts.UnionWith( newPosts );
                    }
                    if ( module.Settings.PostTypes.HasFlag( PostType.Hot ) ) {
                        posts.UnionWith( hotPosts );
                    }
                    if ( module.Settings.PostTypes.HasFlag( PostType.Rising ) ) {
                        posts.UnionWith( risingPosts );
                    }
                }
                List<Post> postsList = new List<Post>();
                if ( !module.MultiScan ) {
                    //only add unseen posts
                    postsList.AddRange( posts.Where( ph => alreadyProcessed.Count( ap => ap.ThingID == ph.Id && ap.SeenByModules.HasFlag( module.ModuleEnum ) ) == 0 ) );
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
                ProcessedItem original = alreadyProcessed.SingleOrDefault( p => p.ThingID == combinedAnalysis.Post.Id );
                if ( original == null ) {
                    original = new ProcessedItem( Subreddit, combinedAnalysis.Post.Id, "invalid", AnalyzableTypes.Post );
                    unseen = true;
                }
                else {
                    var prevScores = original.AnalysisDetails.Scores.Where( os => combinedAnalysis.Scores.Count( cs => cs.Module == os.Module ) == 0 ).ToList();
                    combinedAnalysis.Scores.AddRange( prevScores );
                    combinedAnalysis.AnalyzingModule = original.SeenByModules | combinedAnalysis.AnalyzingModule;
                }
                if ( combinedAnalysis.TotalScore >= Settings.RemoveScoreThreshold && Settings.RemoveScoreThreshold > 0 ) {
                    ProcessedItem removed = removedPreviously.SingleOrDefault( p => p.ThingID == combinedAnalysis.Post.Id );
                    if ( removed == null || removed.AnalysisDetails.TotalScore < combinedAnalysis.TotalScore ) {
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
                    if ( reportedPreviously.Count( p => p.ThingID == combinedAnalysis.Post.Id ) == 0 ) {
                        //can't change report text or report an item again. Thanks Obama... err... Reddit...
                        combinedAnalysis.Post.Report( VotableThing.ReportType.Other, combinedAnalysis.ReportReason );
                        reportedCounter++;
                    }
                    action = "Report";
                }
                if ( combinedAnalysis.TotalScore != original.AnalysisDetails.TotalScore || action != original.Action || original.SeenByModules != combinedAnalysis.AnalyzingModule ) {
                    var updatedItem = original;
                    if ( combinedAnalysis.TotalScore > 0 ) {
                        updatedItem.AnalysisDetails = combinedAnalysis;
                    }
                    else {
                        updatedItem.AnalysisDetails = null;
                    }

                    updatedItem.SeenByModules = updatedItem.SeenByModules | combinedAnalysis.AnalyzingModule;
                    updatedItem.Action = action;
                    //processed post needs updated in
                    if ( unseen ) {
                        try {
                            await processedDAL.LogProcessedItemAsync( updatedItem ); //TODO
                        }
                        catch ( Exception ex ) {
                            Console.WriteLine( "Error adding new post as processed. Messaage : {0}", "\r\n Inner Exception : " + ex.InnerException?.Message );
                        }
                    }
                    else {
                        try {
                            await processedDAL.UpdatedAnalysisScoresAsync( updatedItem.ThingID, Subreddit, updatedItem.AnalysisDetails.Scores ); //TODO
                        }
                        catch ( Exception ex ) {
                            Console.WriteLine( "Error updating processed post. Messaage : {0}", "\r\n Inner Exception : " + ( ex.InnerException != null ? ex.InnerException.Message : "null" ) );
                        }
                    }
                }
            }

            Console.WriteLine( $"Successfully processed {results.Keys.Count} posts.\r\nIgnored posts: {ignoredCounter}\r\nReported Posts: {reportedCounter}\r\nRemoved Posts: {removedCounter}" );

        }
        internal async Task<AnalysisDetails> AnalyzePost( Post post ) {
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

        private void LoadModules() {
            ActiveModules.Clear();
            /*** Load Modules ***/
            if ( Settings.LicensingSmasher.Enabled ) ActiveModules.Add( new LicensingSmasher( Settings.LicensingSmasher, client, Subreddit ) );
            if ( Settings.YouTubeSpamDetector.Enabled ) ActiveModules.Add( new YouTubeSpamDetector( Settings.YouTubeSpamDetector, client, Subreddit ) );
            //if ( Settings.UserStalker.Enabled ) ActiveModules.Add( new UserStalker( Settings.UserStalker, client, Subreddit ) );
            if ( Settings.SelfPromotionCombustor.Enabled ) ActiveModules.Add( new SelfPromotionCombustor( Settings.SelfPromotionCombustor, client ) );
            if ( Settings.HighTechBanHammer.Enabled ) ActiveModules.Add( new HighTechBanHammer( Settings.HighTechBanHammer, client.GetSubreddit( Subreddit ) ) );
            /*** End Load Modules ***/
        }
    }
}
