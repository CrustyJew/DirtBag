using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Extensions.Bindings;
using EasyNetQ;
using System.Text;
using Newtonsoft.Json;
using Npgsql;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DirtbagInboxParser {
    internal class RabbitMQAttributeBindingProvider : ITriggerBindingProvider {
        private string _connString;
        private EasyNetQ.IAdvancedBus _bus;
        public RabbitMQAttributeBindingProvider( string connString) {
            _connString = connString;
            _bus = EasyNetQ.RabbitHutch.CreateBus(_connString,
                    x => {
                        x.Register<ISerializer, DirtbagRabbitSerializer>()
                         .Register<ITypeNameSerializer>(_ => new DirtbagTypeNameSerializer());
                        //if(logger != null) x.Register<IEasyNetQLogger>(_ => logger);
                    }).Advanced;
        }
        public Task<ITriggerBinding> TryCreateAsync( TriggerBindingProviderContext context ) {
            if(context == null) {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            RabbitMQAttribute attribute = parameter.GetCustomAttribute<RabbitMQAttribute>(inherit: false);
            if(attribute == null) {
                return Task.FromResult<ITriggerBinding>(null);
            }

            // TODO: Define the types your binding supports here
            //if(parameter.ParameterType != typeof(Things.PrivateMessage) ) {
            //    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
            //        "Can't bind SampleTriggerAttribute to type '{0}'.", parameter.ParameterType));
            //}

            return Task.FromResult<ITriggerBinding>(new RabbitMQBinding(_bus,parameter, attribute.Queue, attribute.Exchange, attribute.RoutingKey));
        }
        public class DirtbagTypeNameSerializer : ITypeNameSerializer {
            public Type DeSerialize( string typeName ) {
                return typeof(Dirtbag.Models.RabbitAnalysisRequestMessage);
            }

            public string Serialize( Type type ) {
                return "DirtbagWebservice.Models.RabbitAnalysisRequestMessage";
            }
        }

        public class DirtbagMessageSerializer : DefaultMessageSerializationStrategy {
            public DirtbagMessageSerializer( ISerializer serializer, ICorrelationIdGenerationStrategy correlationIdGenerator ) : base(new DirtbagTypeNameSerializer(), serializer, correlationIdGenerator) {
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
                if(message == null) throw new ArgumentNullException("message");
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings));
            }

            public T BytesToMessage<T>( byte[] bytes ) {
                if(bytes == null) throw new ArgumentNullException("bytes");
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), serializerSettings);
            }

            public object BytesToMessage( string typeName, byte[] bytes ) {
                if(bytes == null) throw new ArgumentNullException("bytes");
                typeName = typeName ?? typeof(Dirtbag.Models.RabbitAnalysisRequestMessage).ToString();
                var type = typeNameSerializer.DeSerialize(typeName);
                return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type, serializerSettings);
            }



        }
        private class RabbitMQBinding : ITriggerBinding {
            private readonly ParameterInfo _parameter;
            private readonly IReadOnlyDictionary<string, Type> _bindingContract;
            private readonly EasyNetQ.IAdvancedBus _bus;
            private readonly string _queue;
            private readonly string _exchange;
            private readonly string _routingKey;
            public RabbitMQBinding( EasyNetQ.IAdvancedBus bus, ParameterInfo parameter, string queue, string exchange, string routingKey ) {
                _parameter = parameter;
                _bindingContract = CreateBindingDataContract();
                _bus = bus;
                _queue = queue;
                _exchange = exchange;
                _routingKey = routingKey;
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract {
                get { return _bindingContract; }
            }

            public Type TriggerValueType {
                get { return typeof(Dirtbag.Models.RabbitAnalysisRequestMessage); }
            }

            public Task<ITriggerData> BindAsync( object value, ValueBindingContext context ) {
                // TODO: Perform any required conversions on the value
                // E.g. convert from Dashboard invoke string to our trigger
                // value type
                Dirtbag.Models.RabbitAnalysisRequestMessage triggerValue = value as Dirtbag.Models.RabbitAnalysisRequestMessage;
                IValueBinder valueBinder = new RabbitMQMessageBinder(_parameter, triggerValue);
                return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
            }

            public Task<IListener> CreateListenerAsync( ListenerFactoryContext context ) {
                return Task.FromResult<IListener>(new Listener(context.Executor,_bus, _queue, _exchange, _routingKey ));
            }

            public ParameterDescriptor ToParameterDescriptor() {
                return new SampleTriggerParameterDescriptor {
                    Name = _parameter.Name,
                    DisplayHints = new ParameterDisplayHints {
                        // TODO: Customize your Dashboard display strings
                        Prompt = "Sample",
                        Description = "Sample trigger fired",
                        DefaultValue = "Sample"
                    }
                };
            }

            private IReadOnlyDictionary<string, object> GetBindingData( Dirtbag.Models.RabbitAnalysisRequestMessage value ) {
                Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("RabbitMQSentinelTrigger", value);

                // TODO: Add any additional binding data

                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract() {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("SampleTrigger", typeof(Dirtbag.Models.RabbitAnalysisRequestMessage));

                // TODO: Add any additional binding contract members

                return contract;
            }

            private class SampleTriggerParameterDescriptor : TriggerParameterDescriptor {
                public override string GetTriggerReason( IDictionary<string, string> arguments ) {
                    // TODO: Customize your Dashboard display string
                    return string.Format("RabbitMQ trigger fired at {0}", DateTime.Now.ToString("o"));
                }
            }

            private class RabbitMQMessageBinder : ValueBinder {
                private readonly object _value;

                public RabbitMQMessageBinder( ParameterInfo parameter, Dirtbag.Models.RabbitAnalysisRequestMessage value )
                    : base(parameter.ParameterType) {
                    _value = value;
                }

                public override Task<object> GetValueAsync() {
                    // TODO: Perform any required conversions
                    if(Type == typeof(string)) {
                        return Task.FromResult<object>(_value.ToString());
                    }
                    return Task.FromResult(_value);
                }

                public override string ToInvokeString() {
                    // TODO: Customize your Dashboard invoke string
                    return "Sample";
                }
            }

            private class Listener : IListener {
                private ITriggeredFunctionExecutor _executor;
                private IAdvancedBus _bus;
                private string _queue;
                private string _exchange;
                private string _routingKey;
                private IDisposable _consumer;

                public Listener( ITriggeredFunctionExecutor executor, EasyNetQ.IAdvancedBus bus, string queue, string exchange, string routingKey ) {
                    _executor = executor;
                    _bus = bus;
                    _queue = queue;
                    _exchange = exchange;
                    _routingKey = routingKey;
                }

                public Task StartAsync( CancellationToken cancellationToken ) {
                    // TODO: Start monitoring your event source
                    var exchange = _bus.ExchangeDeclare(_exchange, EasyNetQ.Topology.ExchangeType.Direct);
                    var queue = _bus.QueueDeclare(_queue);
                    var binding = _bus.Bind(exchange, queue, _routingKey);

                    _consumer = _bus.Consume<Dirtbag.Models.RabbitAnalysisRequestMessage>(queue, this.Subscribe, conf => { conf.WithPrefetchCount(25); });
                   
                    return Task.FromResult(true);
                }

                private async Task Subscribe( IMessage<Dirtbag.Models.RabbitAnalysisRequestMessage> request, MessageReceivedInfo info ) {
                    try {
                        await _executor.TryExecuteAsync(new TriggeredFunctionData {
                            TriggerValue = request.Body
                        },CancellationToken.None);
                    }
                    catch(Exception ex) {
                        Console.WriteLine($"Error in analysis for {request.Body.Subreddit} {request.Body.ThingID}. {ex.Message} \r\n {ex.StackTrace}");
                        throw new EasyNetQ.EasyNetQException("Failed to analyze..", ex);
                    }
                }

                public Task StopAsync( CancellationToken cancellationToken ) {
                    // TODO: Stop monitoring your event source
                    _consumer?.Dispose();
                    return Task.FromResult(true);
                }

                public void Dispose() {
                    // TODO: Perform any final cleanup
                    _consumer?.Dispose();
                }

                public void Cancel() {
                    // TODO: cancel any outstanding tasks initiated by this listener
                }

                private void OnTimer( object sender, System.Timers.ElapsedEventArgs e ) {
                    // TODO: When you receive new events from your event source,
                    // invoke the function executor
                    TriggeredFunctionData input = new TriggeredFunctionData {
                        TriggerValue = null
                    };
                    _executor.TryExecuteAsync(input, CancellationToken.None).Wait();
                }
            }

           
        }
    }
}
