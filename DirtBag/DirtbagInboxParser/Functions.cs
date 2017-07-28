using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using RedditSharp.Azure;
using RedditSharp.Things;
using System.Data.SqlClient;

namespace DirtbagInboxParser {
    public class Functions {
        public static RedditSharp.Reddit redditClient;
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage( [QueueTrigger("queue")] string message2, TextWriter log ) {
            log.WriteLine(message2);
        }
        
        public static async Task ProcessRedditMessage ( [RedditMessage(false, MessageType.PrivateMessage)] RedditSharp.Things.PrivateMessage message ) {
            string subject = message.Subject.ToLower();
            List<string> args = subject.Split('-').Select(p => p.Trim()).ToList();

            bool force = args.Count > 1 && args.Contains("force");
            if(!subject.Contains("validate") && !subject.Contains("check") &&
                !subject.Contains("analyze") && !subject.Contains("test") && !subject.Contains("verify")) {
                await message.ReplyAsync("Whatchu talkin bout Willis");
                return;
            }
            Post post;
            RedditSharp.Reddit reddit = new RedditSharp.Reddit(Program.BotAgent, false);
            try {
                post = await reddit.GetPostAsync(new Uri(message.Body));
            }
            catch {
                await message.ReplyAsync("That URL made me throw up in my mouth a little. Try again!");
                return;
            }

            string subreddit = post.SubredditName;

            var mods = await Subreddit.GetModeratorsAsync(Program.BotAgent, subreddit);

            if(!mods.Any(m=>m.Name.ToLower() == message.AuthorName.ToLower())) {
                await message.ReplyAsync($"You aren't a mod of {post.SubredditName}! What are you doing here? Go on! GIT!");
                return;
            }

            var dirtbagDAL = new Dirtbag.DAL.ProcessedItemSQLDAL(new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["Dirtbag"].ConnectionString));
            var dirtbagBLL = new Dirtbag.BLL.ProcessedItemBLL(dirtbagDAL);

            var sentinelDAL = new Dirtbag.DAL.SentinelChannelBanDAL(new Npgsql.NpgsqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["SentinelDirtbag"].ConnectionString));
            var sentinelBLL = new Dirtbag.BLL.SentinelChannelBanBLL(sentinelDAL);

            var analysis = await dirtbagBLL.ReadThingAnalysis(post.FullName, subreddit);

            var channelban = await sentinelBLL.CheckSentinelChannelBan(subreddit, post.Id);

            var reply = new StringBuilder();
            reply.AppendLine("##Sentinel Bot Banned Media");
            reply.AppendLine();
            if(channelban == null || channelban.Count() == 0) {
                reply.AppendLine("No banned media channels detected!");
            }
            else {
                reply.AppendLine("Channel Name| Media Platform | Blacklisted By | Blacklisted On UTC | Global Ban");
                reply.AppendLine(":--|:--:|:--:|:--:|:--");
                foreach(var ban in channelban) {
                    reply.AppendLine($"[{ban.MediaAuthor}](https://layer7.solutions/blacklist/reports/#type=channel&subject={ban.MediaChannelID})|{ban.MediaPlatform}|{ban.BlacklistBy}|{ban.BlacklistDateUTC}|{(ban.GlobalBan ? "GLOBAL" : "")}");
                }
            }
            reply.AppendLine();
            reply.AppendLine("-----------");
            reply.AppendLine();
            if(analysis != null) {
                reply.AppendLine(
                    $"Analysis results for \"[{post.Title}]({post.Permalink})\" submitted by /u/{analysis.Author} to /r/{post.SubredditName}");
                reply.AppendLine();
                reply.AppendLine($"##Action Taken: {analysis.Action} with a score of {analysis.HighScore}");
                foreach(var result in analysis.Analysis) {
                    reply.AppendLine();
                    reply.AppendLine($"##Media Platform: {result.MediaPlatform} | Media ID: {result.MediaID} | Channel:[{result.MediaChannelName}](https://layer7.solutions/blacklist/reports/#type=channel&subject={result.MediaChannelID})");
                    reply.AppendLine();
                    reply.AppendLine("Module| Score |Reason");
                    reply.AppendLine(":--|:--:|:--");
                    foreach(var score in result.Scores) { 
                        reply.AppendLine($"{score.Module}|{score.Score}|{score.Reason}");
                    }
                }
            }
            string replyString = "";
            if(reply.Length > 10000) { replyString = reply.ToString(0, 10000); }
            else { replyString = reply.ToString(); }
            await message.ReplyAsync(replyString);

        }

        public static async Task ReEvaluateLicensing ( [TimerTrigger("00:05:00")] TimerTriggerAttribute trigger ) {
            
        }
    }
}
