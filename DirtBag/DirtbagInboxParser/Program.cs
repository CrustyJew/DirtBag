using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using RedditSharp.Azure;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace DirtbagInboxParser {
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    public class Program {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static RedditSharp.BotWebAgent DirtbagAgent;
        public static MemoryCache MemCache;
        public static RedditSharp.WebAgentPool<string, RedditSharp.BotWebAgent> BotAgentPool;
        public static IConfigurationRoot ConfigRoot;
        static void Main() {
            BotAgentPool = new RedditSharp.WebAgentPool<string, RedditSharp.BotWebAgent>();
            ConfigRoot = new ConfigurationBuilder().AddJsonFile("PrivateConfig.json", true).Build();
            MemCache = new MemoryCache(new MemoryCacheOptions());

            DirtbagAgent = new RedditSharp.BotWebAgent(ConfigRoot["BotUsername"], ConfigRoot["BotPassword"], ConfigRoot["BotClientID"], ConfigRoot["BotClientSecret"], ConfigRoot["BotRedirectURI"]);

            var config = new JobHostConfiguration();
            if(config.IsDevelopment) {
                config.UseDevelopmentSettings();
            }
            config.UseTimers();
            config.UseRedditMessageTrigger(DirtbagAgent);
            config.UseRabbitMQTrigger(System.Configuration.ConfigurationManager.ConnectionStrings["Rabbit"].ConnectionString);
            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}
