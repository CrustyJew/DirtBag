using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using RedditSharp.Azure;
using Microsoft.Azure.WebJobs.Extensions;

namespace DirtbagInboxParser {
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    public class Program {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static RedditSharp.BotWebAgent BotAgent;
        static void Main() {
            var config = new JobHostConfiguration();

            if(config.IsDevelopment) {
                config.UseDevelopmentSettings();
            }
            config.UseRedditMessageTrigger(BotAgent);
            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}
