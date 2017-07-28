using System;

using Microsoft.Azure.WebJobs.Description;
namespace DirtbagInboxParser{
    [AttributeUsage(AttributeTargets.Parameter)][Binding]
    public sealed class RabbitMQAttribute : Attribute {
        public RabbitMQAttribute( string queue, string exchange, string routingKey ) {
            Queue = queue;
            Exchange = exchange;
            RoutingKey = routingKey;
        }
        public string Queue { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }

    }
}

