using CommunityToolkit.HighPerformance.Buffers;
using PooledAwait;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace SolaceSharp.Serialisation
{
    internal class MessageTransformer
    {
        private readonly ISerializerRegistry _registry;
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create(1024, 256);
        private readonly ConcurrentDictionary<string, ITopic> _topics = new ConcurrentDictionary<string, ITopic>();

        public MessageTransformer(ISerializerRegistry registry)
        {
            _registry = registry;
        }

        public IMessage Transform<T>(
            T payload,
            PublishMeta meta)
        {
            var serialiser = _registry.GetSerializer<T>();
            using (var writer = new ArrayPoolBufferWriter<byte>(_pool))
            {
                serialiser.Serialise(writer, payload);
                return CreateMessage(writer.WrittenSpan.ToArray(), meta);
            }
        }

        public T Transform<T>(IMessage message)
        {
            var serialiser = _registry.GetSerializer<T>();
            using (var writer = new ArrayPoolBufferWriter<byte>(_pool))
            {
                return serialiser.Deserialise(message.BinaryAttachment);
            }
        }

        private IMessage CreateMessage(byte[] buffer, PublishMeta meta)
        {
            if (!_topics.TryGetValue(meta.Topic, out var top))
            {
                top = ContextFactory.Instance.CreateTopic(meta.Topic);
                _topics[meta.Topic] = top;
            }

            var msg = Pool.TryRent<IMessage>() ?? ContextFactory.Instance.CreateMessage();
            var correlationKey = Guid.NewGuid();
            msg.DeliveryMode = MessageDeliveryMode.Persistent;
            msg.BinaryAttachment = buffer;
            msg.Destination = top;
            msg.CorrelationKey = correlationKey;
            msg.CorrelationId = correlationKey.ToString();
            msg.AckImmediately = meta.FireAndForget ? false : true;
            return msg;
        }

        public void Return(IMessage message)
        {
            Pool.Return(message);
        }
    }
}
