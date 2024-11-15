
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SolaceSharp.Internal;
using SolaceSharp.Serialisation;
using SolaceSystems.Solclient.Messaging;

namespace SolaceSharp
{
    public struct PublishMeta
    {
        public PublishMeta(
            string topic = "",
            bool fireAndForget = false,
            bool isReply = false)
        {
            Topic = topic;
            FireAndForget = fireAndForget;
            IsReply = isReply;
        }

        public bool FireAndForget { get; private set; }
        public string Topic { get; private set; }
        public bool IsReply { get; private set; }
    }

    public interface ISolaceClient
    {
        ValueTask ConnectAsync(CancellationToken token);
        ValueTask PublishAsync<T>(T payload, PublishMeta meta, CancellationToken cancellationToken);
        ValueTask<PublishFuture> PublishConcurrentAsync<T>(T payload, PublishMeta meta, CancellationToken cancellationToken);
        ValueTask<ISubscription<T>> SubscribeAsync<T>(string topic, CancellationToken token);
        ValueTask<TOut> SendRequest<TIn, TOut>(TIn request, PublishMeta meta, TimeSpan timeout, CancellationToken cancellationToken);

        // Persistent messaging
        /*   Task CreateQueue(QueueConfig config, CancellationToken token);
           Task CreateOrUpdateQueue(QueueConfig config, CancellationToken token);
           Task PurgeQueue(CancellationToken token);
           Task DeleteQueue(CancellationToken token);
           Task<QueueConsumer> ConsumeQueue();
        */

    }

    public class SolaceClient : ISolaceClient
    {
        private readonly SessionProperties _sessionProperties;
        private readonly MessageTransformer _transformer;
        private IContext _context;
        private SessionWrapper _session;

        public SolaceClient(SessionProperties properties, ISerializerRegistry serialiserRegister = null)
        {
            _sessionProperties = properties;
            _transformer = new MessageTransformer(serialiserRegister ?? new JsonSerializerRegistry());

            ContextFactoryProperties cfp = new ContextFactoryProperties()
            {
                SolClientLogLevel = SolLogLevel.Warning
            };
            cfp.LogToConsoleError();
            ContextFactory.Instance.Init(cfp);

            _context = ContextFactory.Instance.CreateContext(new ContextProperties(), HandleContextEvent);
        }

        public ValueTask ConnectAsync(CancellationToken token = default)
        {
            var inner = _context.CreateSession(_sessionProperties, HandleMessageEvent, HandleSessionEvent);
            _session = new SessionWrapper(inner);
            return _session.ConnectAsync(token);
        }

        public async ValueTask PublishAsync<T>(
            T payload,
            PublishMeta meta,
            CancellationToken cancellationToken = default)
        {
            var msg = _transformer.Transform(payload, meta);

            try
            {
                await _session.SendAsync(msg, meta.FireAndForget, cancellationToken);
            }
            finally
            {
                _transformer.Return(msg);
            }
        }

        public async ValueTask<PublishFuture> PublishConcurrentAsync<T>(
            T payload,
            PublishMeta meta,
            CancellationToken cancellationToken = default)
        {
            var msg = _transformer.Transform(payload, meta);
            return await _session.SendConcurrentAsync(msg, cancellationToken);
        }

        public async ValueTask<ISubscription<T>> SubscribeAsync<T>(string topic, CancellationToken token = default)
        {
            return await _session.SubscribeAsync<T>(topic, _transformer, token);
        }

        public async ValueTask<TOut> SendRequest<TIn, TOut>(
            TIn request,
            PublishMeta meta,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default)
        {
            var msg = _transformer.Transform(request, meta);
            try
            {
                var reply = await _session.SendRequest(msg, timeout, cancellationToken);
                return _transformer.Transform<TOut>(reply);
            }
            finally
            {
                _transformer.Return(msg);
            }

        }

        private void HandleContextEvent(object s, ContextEventArgs msg)
        {
            Console.WriteLine("Message Received");
        }

        private void HandleMessageEvent(object s, MessageEventArgs msg) => _session.HandleMessageEvent(s, msg);

        private void HandleSessionEvent(object s, SessionEventArgs args) => _session.HandleSessionEvent(s, args);
    }
}