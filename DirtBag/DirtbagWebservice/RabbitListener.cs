using EasyNetQ;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace DirtbagWebservice
{
    public class RabbitListener
    {
        private System.IServiceProvider provider;
        private ILogger<RabbitListener> logger;
        private BLL.IAnalyzeMediaBLL analysisBLL;
        public RabbitListener( System.IServiceProvider provider, ILogger<RabbitListener> logger ) {
            this.provider = provider;
            this.logger = logger;
        }

        public async Task Subscribe(IMessage<Models.RabbitAnalysisRequestMessage> request, MessageReceivedInfo info ) {
            var analysisBLL = (BLL.IAnalyzeMediaBLL) provider.GetService( typeof( BLL.IAnalyzeMediaBLL ) );
            try {
                var results = await analysisBLL.AnalyzeMedia(request.Body.Subreddit, request.Body, true).ConfigureAwait(false);
                //if ( results.RequiredAction != Models.AnalysisResults.Action.Nothing || !returnActionsOnly ) {
                //    await rabbitBus.PublishAsync( resultsExchange, resultRoutingKey, true, new Message<Models.AnalysisResults>( results ) );
                //}
            }
            catch(Exception ex) {
                logger.LogError($"Error in analysis for {request.Body.Subreddit} {request.Body.ThingID}. {ex.Message} \r\n {ex.StackTrace}");
                throw new EasyNetQ.EasyNetQException( "Failed to analyze..", ex );
            }
        }
    }

    public class DirtbagTypeNameSerializer : ITypeNameSerializer {
        public Type DeSerialize( string typeName ) {
            return typeof( Models.RabbitAnalysisRequestMessage );
        }

        public string Serialize( Type type ) {
            return "DirtbagWebservice.Models.RabbitAnalysisRequestMessage";
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
