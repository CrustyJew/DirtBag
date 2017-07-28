using System;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs;

namespace RedditSharp.Azure {
    public static class RedditMessageListenerConfigurationExtensions {
        public static void UseRedditMessageTrigger( this JobHostConfiguration config, BotWebAgent webAgent ) {
            if(config == null) {
                throw new ArgumentNullException("config");
            }

            // Register our extension configuration provider
            config.RegisterExtensionConfigProvider(new RedditMessageListenerExtensionConfig(webAgent));
        }

        private class RedditMessageListenerExtensionConfig : IExtensionConfigProvider {
            private RedditSharp.BotWebAgent _botAgent;
            public RedditMessageListenerExtensionConfig( RedditSharp.BotWebAgent botAgent ) {
                _botAgent = botAgent;
            }
            public void Initialize( ExtensionConfigContext context ) {
                if(context == null) {
                    throw new ArgumentNullException("context");
                }

                // Register our extension binding providers
                context.Config.RegisterBindingExtensions(
                    new RedditMessageAttributeBindingProvider(_botAgent));
            }
        }
    }
}