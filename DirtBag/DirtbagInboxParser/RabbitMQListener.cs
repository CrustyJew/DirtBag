using System;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs;

namespace DirtbagInboxParser {
    public static class RabbitMQListenerConfigurationExtensions {
        public static void UseRabbitMQTrigger( this JobHostConfiguration config, string connectionString) {
            if(config == null) {
                throw new ArgumentNullException("config");
            }

            // Register our extension configuration provider
            config.RegisterExtensionConfigProvider(new RabbitMQListenerExtensionConfig(connectionString));
        }

        private class RabbitMQListenerExtensionConfig : IExtensionConfigProvider {
            private string _connString;
            public RabbitMQListenerExtensionConfig( string connString ) {
                _connString = connString;
            }
            public void Initialize( ExtensionConfigContext context ) {
                if(context == null) {
                    throw new ArgumentNullException("context");
                }

                // Register our extension binding providers
                context.Config.RegisterBindingExtensions(
                    new RabbitMQAttributeBindingProvider(_connString));
            }
        }
    }
}