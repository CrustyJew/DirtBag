﻿using EasyNetQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
[assembly: UserSecretsId( "aspnet-RabbitRequeue-20170306082242" )]
namespace RabbitRequeue
{
    public class Program
    {
        public static IAdvancedBus rabbit;
        public static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" );
            var builder = new ConfigurationBuilder()
                   .AddEnvironmentVariables();

            if ( environmentName == "Development" ) {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }
            var Configuration = builder.Build();
            
            var logger = new EasyNetQ.Loggers.ConsoleLogger(); 
            var rabbit = RabbitHutch.CreateBus( Configuration.GetConnectionString( "Rabbit" ), x => x.Register<IEasyNetQLogger>( _ => logger ).Register<ITypeNameSerializer>( _ => new DirtbagTypeNameSerializer() ) ).Advanced;
            var exchange = rabbit.ExchangeDeclare( "ErrorExchange_" + Configuration["RabbitQueue"], EasyNetQ.Topology.ExchangeType.Direct );
            var queue = rabbit.QueueDeclare( "EasyNetQ_Default_Error_Queue" );

            rabbit.Consume<EasyNetQ.SystemMessages.Error>( queue, async ( m, i ) => {
                string msg = m.Body.Message.Replace( "\"{", "{" ).Replace("}\"","}").Replace(@"\","");
                await rabbit.PublishAsync( rabbit.ExchangeDeclare( m.Body.Exchange, EasyNetQ.Topology.ExchangeType.Direct ), m.Body.RoutingKey, true, new Message<DirtBagWebservice.Models.AnalysisRequest>(Newtonsoft.Json.JsonConvert.DeserializeObject<DirtBagWebservice.Models.AnalysisRequest>( msg ) ) );
            } );
        }
    }
    public class DirtbagTypeNameSerializer : ITypeNameSerializer {
        public Type DeSerialize( string typeName ) {
            return typeof( EasyNetQ.SystemMessages.Error );
        }

        public string Serialize( Type type ) {
            return "DirtBagWebservice.Models.RabbitAnalysisRequestMessage";
        }
    }
}
