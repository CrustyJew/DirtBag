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
using RedditSharp.Things;
using RedditSharp.Azure;

namespace RedditSharp.Azure {
    internal class RedditMessageAttributeBindingProvider : ITriggerBindingProvider {

        private RedditSharp.BotWebAgent _botAgent;
        public RedditMessageAttributeBindingProvider(BotWebAgent botAgent) {
            _botAgent = botAgent;
        }
        public Task<ITriggerBinding> TryCreateAsync( TriggerBindingProviderContext context ) {
            if(context == null) {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            RedditMessageAttribute attribute = parameter.GetCustomAttribute<RedditMessageAttribute>(inherit: false);
            if(attribute == null) {
                return Task.FromResult<ITriggerBinding>(null);
            }

            // TODO: Define the types your binding supports here
            if(parameter.ParameterType != typeof(Things.PrivateMessage) ) {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind SampleTriggerAttribute to type '{0}'.", parameter.ParameterType));
            }

            return Task.FromResult<ITriggerBinding>(new RedditMessageBinding(_botAgent, attribute.MarkAsRead, attribute.MessageTypes, context.Parameter));
        }

        private class RedditMessageBinding : ITriggerBinding {
            private readonly ParameterInfo _parameter;
            private readonly IReadOnlyDictionary<string, Type> _bindingContract;
            private readonly BotWebAgent _botAgent;
            private readonly bool _markAsRead;
            private readonly MessageType _msgTypes;
            public RedditMessageBinding( RedditSharp.BotWebAgent botAgent , bool markAsRead, MessageType msgTypes, ParameterInfo parameter ) {
                _parameter = parameter;
                _bindingContract = CreateBindingDataContract();
                _botAgent = botAgent;
                _markAsRead = markAsRead;
                _msgTypes = msgTypes;
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract {
                get { return _bindingContract; }
            }

            public Type TriggerValueType {
                get { return typeof(Things.PrivateMessage); }
            }

            public Task<ITriggerData> BindAsync( object value, ValueBindingContext context ) {
                // TODO: Perform any required conversions on the value
                // E.g. convert from Dashboard invoke string to our trigger
                // value type
                Things.PrivateMessage triggerValue = value as Things.PrivateMessage;
                IValueBinder valueBinder = new PrivateMessageBinder(_parameter, triggerValue);
                return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
            }

            public Task<IListener> CreateListenerAsync( ListenerFactoryContext context ) {
                return Task.FromResult<IListener>(new Listener(context.Executor, _botAgent, _markAsRead, _msgTypes));
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

            private IReadOnlyDictionary<string, object> GetBindingData( Things.PrivateMessage value ) {
                Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("SampleTrigger", value);

                // TODO: Add any additional binding data

                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract() {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("SampleTrigger", typeof(Things.PrivateMessage));

                // TODO: Add any additional binding contract members

                return contract;
            }

            private class SampleTriggerParameterDescriptor : TriggerParameterDescriptor {
                public override string GetTriggerReason( IDictionary<string, string> arguments ) {
                    // TODO: Customize your Dashboard display string
                    return string.Format("Sample trigger fired at {0}", DateTime.Now.ToString("o"));
                }
            }

            private class PrivateMessageBinder : ValueBinder {
                private readonly object _value;

                public PrivateMessageBinder( ParameterInfo parameter, Things.PrivateMessage value )
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
                private BotWebAgent _botAgent;
                private Reddit _reddit;
                private IObserver<RedditSharp.Things.PrivateMessage> observer;
                private IDisposable unobserve;
                private bool _markAsRead;
                private MessageType _msgTypes;
                public Listener( ITriggeredFunctionExecutor executor, BotWebAgent botAgent, bool markAsRead, MessageType msgTypes ) {
                    _executor = executor;
                    _botAgent = botAgent;
                    // TODO: For this sample, we're using a timer to generate
                    // trigger events. You'll replace this with your event source.
                    _reddit = new Reddit(_botAgent, true);
                    _markAsRead = markAsRead;
                    _msgTypes = msgTypes;
                }

                public Task StartAsync( CancellationToken cancellationToken ) {
                    // TODO: Start monitoring your event source
                    observer = new MessageObserver(_executor, _markAsRead, _msgTypes);
                    var stream = _reddit.User.GetInbox().Stream();

                    unobserve = stream.Subscribe(observer);
                    return stream.Enumerate(cancellationToken);
                    
                    //return Task.FromResult(true);
                }

                public Task StopAsync( CancellationToken cancellationToken ) {
                    // TODO: Stop monitoring your event source
                    unobserve?.Dispose();
                    return Task.FromResult(true);
                }

                public void Dispose() {
                    // TODO: Perform any final cleanup
                    unobserve?.Dispose();
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

            private class MessageObserver : IObserver<Things.PrivateMessage> {
                private bool _markAsRead;
                private MessageType _msgTypes;
                private ITriggeredFunctionExecutor _executor;
                public MessageObserver(ITriggeredFunctionExecutor executer, bool markAsRead, MessageType msgTypes ) {
                    _executor = executer;
                    _markAsRead = markAsRead;
                    _msgTypes = msgTypes;
                }
                public void OnNext( PrivateMessage value ) {
                    TriggeredFunctionData data = new TriggeredFunctionData {
                        TriggerValue = value
                    };
                    if(!value.Unread) return;
                    if(value.IsComment) {
                        switch(value.Subject) {
                            case "comment reply":
                                if(_msgTypes.HasFlag(MessageType.CommentReply)) {
                                    _executor.TryExecuteAsync(data, CancellationToken.None).Wait();
                                }break;
                            case "post reply":
                                if(_msgTypes.HasFlag(MessageType.PostReply)) {
                                    _executor.TryExecuteAsync(data, CancellationToken.None).Wait();
                                }
                                break;
                            case "username mention":
                                if(_msgTypes.HasFlag(MessageType.UsernameMention)) {
                                    _executor.TryExecuteAsync(data, CancellationToken.None).Wait();
                                }
                                break;
                        }
                    }
                    else if(_msgTypes.HasFlag(MessageType.PrivateMessage)) {
                        _executor.TryExecuteAsync(data, CancellationToken.None).Wait();
                    }

                    if(_markAsRead) {
                        value.SetAsReadAsync().Wait();
                    }
                }

                public void OnError( Exception error ) {
                    throw new NotImplementedException();
                }

                public void OnCompleted() {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
