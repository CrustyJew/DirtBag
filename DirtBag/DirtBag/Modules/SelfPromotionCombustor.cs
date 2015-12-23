using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedditSharp.Things;
using DirtBag.Helpers;

namespace DirtBag.Modules {
    class SelfPromotionCombustor : IModule {
        public string ModuleName {
            get {
                return "Self Promotion Combustor";
            }
        }

        public IModuleSettings Settings { get; set; }
        public RedditSharp.Reddit RedditClient { get; set; }
        private Dictionary<string, int> processedCache;

        public SelfPromotionCombustor() {
            processedCache = new Dictionary<string, int>();
        }

        public SelfPromotionCombustor(RedditSharp.Reddit client, SelfPromotionCombustorSettings settings ) : this() {
            RedditClient = client;
            Settings = settings;
        }

        public Task<Dictionary<string, PostAnalysisResults>> Analyze( List<Post> posts ) {
            
            var toReturn = new Dictionary<string, PostAnalysisResults>();

            foreach ( var post in posts.Where( p => !processedCache.Keys.Contains( p.Id ) ) ) {
                var youTubePosts = new Dictionary<string, List<Post>>();

                toReturn.Add( post.Id, new PostAnalysisResults( post ) );
                string ytID = YouTubeHelpers.ExtractVideoId( post.Url.ToString() );

                if ( !string.IsNullOrEmpty( ytID ) ) {
                    if ( !youTubePosts.ContainsKey( ytID ) ) youTubePosts.Add( ytID, new List<Post>() );
                    youTubePosts[ytID].Add( post );
                }
                var recentPosts = RedditClient.Search<RedditSharp.Things.Post>( $"author:{post.AuthorName} self:no",RedditSharp.Sorting.New ).GetListing(100,100);
                foreach(var recentPost in recentPosts ) {
                    ytID = YouTubeHelpers.ExtractVideoId( recentPost.Url.ToString() );

                    if ( !string.IsNullOrEmpty( ytID ) ) {
                        if ( !youTubePosts.ContainsKey( ytID ) ) youTubePosts.Add( ytID, new List<Post>() );
                        youTubePosts[ytID].Add( post );
                    }
                }
            }
            throw new NotImplementedException();
        }
        private void ManageCache( List<Post> posts ) {

            IEnumerable<string> postIDs = posts.Select( p => p.Id );
            foreach ( var notSeen in processedCache.Where( c => !postIDs.Contains( c.Key ) ).ToArray() ) {
                processedCache[notSeen.Key]++;
            }
            foreach ( string id in postIDs ) {
                if ( processedCache.ContainsKey( id ) ) processedCache[id] = 0;
                else processedCache.Add( id, 0 );
            }
            foreach ( var expired in processedCache.Where( c => c.Value > 3 ).ToArray() ) {
                processedCache.Remove( expired.Key );
            }

        }

    }

    class SelfPromotionCombustorSettings : IModuleSettings {
        public bool Enabled { get; set; }

        public int EveryXRuns { get; set; }

        public PostType PostTypes { get; set; }

        public double ScoreMultiplier { get; set; }

        public SelfPromotionCombustorSettings() {
            SetDefaultSettings();
        }

        public void SetDefaultSettings() {
            Enabled = false;
            EveryXRuns = 1;
            PostTypes = PostType.New;
            ScoreMultiplier = 1;
        }
    }
}
