using EasyNetQ;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace DirtBagWebservice
{
    public class RabbitListener
    {
        private System.IServiceProvider provider;
        private IAdvancedBus rabbitBus;
        private IExchange resultsExchange;
        private string resultRoutingKey;
        private bool returnActionsOnly;
        public RabbitListener(System.IServiceProvider provider, IAdvancedBus rabbitBus, IExchange resultsExchange, string resultRoutingKey, bool returnActionsOnly ) {
            this.provider = provider;
            this.rabbitBus = rabbitBus;
            this.resultsExchange = resultsExchange;
            this.resultRoutingKey = resultRoutingKey;
            this.returnActionsOnly = returnActionsOnly;
        }

        public async Task Subscribe(IMessage<Models.RabbitAnalysisRequestMessage> request, MessageReceivedInfo info ) {
            var analysisBLL = (BLL.IAnalyzePostBLL) provider.GetService( typeof( BLL.IAnalyzePostBLL ) );
            try {
                var results = await analysisBLL.AnalyzePost( request.Body.Subreddit, request.Body );
                if ( results.RequiredAction != Models.AnalysisResults.Action.Nothing || !returnActionsOnly ) {
                    await rabbitBus.PublishAsync( resultsExchange, resultRoutingKey, false, new Message<Models.AnalysisResults>( results ) );
                }
            }
            catch(Exception ex) {
                throw new EasyNetQ.EasyNetQException( "Failed to analyze..", ex );
            }
        }
    }

    public class DirtbagTypeNameSerializer : ITypeNameSerializer {
        public Type DeSerialize( string typeName ) {
            return typeof( Models.RabbitAnalysisRequestMessage );
        }

        public string Serialize( Type type ) {
            return "DirtBagWebservice.Models.RabbitAnalysisRequestMessage";
        }
    }

    public class DirtbagMessageSerializer : DefaultMessageSerializationStrategy {
        public DirtbagMessageSerializer( ISerializer serializer, ICorrelationIdGenerationStrategy correlationIdGenerator ) : base( new DirtbagTypeNameSerializer(), serializer, correlationIdGenerator ) {
        }
    }

    public class DirtbagRabbitSerializer : ISerializer {
        private readonly ITypeNameSerializer typeNameSerializer;

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public DirtbagRabbitSerializer( ITypeNameSerializer typeNameSerializer ) {
            this.typeNameSerializer = typeNameSerializer;
        }

        public byte[] MessageToBytes<T>( T message ) where T : class {
            if ( message == null ) throw new ArgumentNullException( "message" );
            return Encoding.UTF8.GetBytes( JsonConvert.SerializeObject( message, serializerSettings ) );
        }

        public T BytesToMessage<T>( byte[] bytes ) {
            if ( bytes == null ) throw new ArgumentNullException( "bytes" );
            return JsonConvert.DeserializeObject<T>( Encoding.UTF8.GetString( bytes ), serializerSettings );
        }

        public object BytesToMessage( string typeName, byte[] bytes ) {
            if ( bytes == null ) throw new ArgumentNullException( "bytes" );
            typeName = typeName ?? typeof( Models.RabbitAnalysisRequestMessage ).ToString();
            var type = typeNameSerializer.DeSerialize( typeName );
            return JsonConvert.DeserializeObject( Encoding.UTF8.GetString( bytes ), type, serializerSettings );
        }
    

        
    }
}
