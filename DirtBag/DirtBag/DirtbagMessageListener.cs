using DirtBag.Models;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag {
    public class DirtbagMessageListener {
        public List<DirtbagBot> DirtbagBots { get; set; }
        private Reddit client;
        private DAL.IProcessedItemDAL processedDAL;

        public DirtbagMessageListener(Reddit redditClient, DAL.IProcessedItemDAL processedItemDAL ) {
            client = redditClient;
            processedDAL = processedItemDAL;
        }

        public void StartListener() {
            Task.Run( () => ProcessMessages() );
        }

        private async Task ProcessMessages() {
            var messages = client.User.UnreadMessages.GetListingStream();

            
            foreach ( var message in messages.Where( unread => unread.Kind == "t4" ).Cast<PrivateMessage>() ) {
                message.SetAsRead();
                string subject = message.Subject.ToLower();
                List<string> args = subject.Split( '-' ).Select( p => p.Trim() ).ToList();

                bool force = args.Count > 1 && args.Contains( "force" );
                if ( !subject.Contains( "validate" ) && !subject.Contains( "check" ) &&
                    !subject.Contains( "analyze" ) && !subject.Contains( "test" ) && !subject.Contains( "verify" ) ) {
                    message.Reply( "Whatchu talkin bout Willis" );
                    continue;
                }
                Post post;
                try {
                    post = client.GetPost( new Uri( message.Body ) );
                }
                catch {
                    message.Reply( "That URL made me throw up in my mouth a little. Try again!" );
                    continue;
                }
                string sub = post.SubredditName.ToLower();
                var bot = DirtbagBots.SingleOrDefault( b => b.Subreddit.ToLower() == sub );
                if ( bot != null ) { 
                    message.Reply( $"I don't have any minions in {post.SubredditName}." );
                    return;
                }
                var mods = new List<string>();
                mods.AddRange( client.GetSubreddit( sub ).Moderators.Select( m => m.Name.ToLower() ).ToList() );

                if ( !mods.Contains( message.Author.ToLower() ) ) {
                    message.Reply( $"You aren't a mod of {post.SubredditName}! What are you doing here? Go on! GIT!" );
                }
                else {
                    //omg finally analyze the damn thing
                    AnalysisDetails result;
                    var original = await processedDAL.ReadProcessedItemAsync( post.Id, sub );
                    if ( !force && original.AnalysisDetails != null ) {
                        result = original.AnalysisDetails;
                    }
                    else if ( post.AuthorName == "[deleted]" ) {
                        message.Reply( "The OP deleted the post, and I don't have it cached so I can't check it. Sorry (read in Canadian accent)!" );
                        continue;
                    }
                    else {
                        result = await bot.AnalyzePost( post );
                    }
                    var reply = new StringBuilder();
                    reply.AppendLine(
                        $"Analysis results for \"[{post.Title}]({post.Permalink})\" submitted by /u/{post.AuthorName} to /r/{post.SubredditName}" );
                    reply.AppendLine();
                    var action = "None";
                    if ( bot.Settings.RemoveScoreThreshold > 0 && result.TotalScore >= bot.Settings.RemoveScoreThreshold ) action = "Remove";
                    else if ( bot.Settings.ReportScoreThreshold > 0 && result.TotalScore >= bot.Settings.ReportScoreThreshold ) action = "Report";
                    reply.AppendLine( $"##Actual Action Taken: {action} with a score of {result.TotalScore}" );
                    reply.AppendLine();
                    reply.AppendLine( $"##Action Based on Current Settings: {action} " );
                    reply.AppendLine();
                    reply.AppendLine(
                        $"**/r/{post.SubredditName}'s thresholds** --- Remove : **{( bot.Settings.RemoveScoreThreshold > 0 ? bot.Settings.RemoveScoreThreshold.ToString() : "Disabled" )}** , Report : **{( bot.Settings.ReportScoreThreshold > 0 ? bot.Settings.ReportScoreThreshold.ToString() : "Disabled" )}**" );
                    reply.AppendLine();
                    reply.AppendLine( "Module| Score |Reason" );
                    reply.AppendLine( ":--|:--:|:--" );
                    foreach ( var score in result.Scores ) {
                        reply.AppendLine( $"{score.Module.ToString()}|{score.Score}|{score.Reason}" );
                    }
                    message.Reply( reply.ToString() );
                }
            }
        }
    }
}
