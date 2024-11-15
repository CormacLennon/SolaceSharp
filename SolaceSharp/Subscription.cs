
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Open.ChannelExtensions;
using SolaceSystems.Solclient.Messaging;
using SolaceSharp.Serialisation;
using SolaceSharp.Internal;

namespace SolaceSharp
{
    public interface ISubscription<T> : IAsyncDisposable
    {
        IAsyncEnumerable<Message<T>> AsAsyncEnumerable(CancellationToken token = default);
    }

    internal class Subscription<T> : ISubscription<T>
    {
        private readonly MessageTransformer _transformer;
        private readonly ChannelReader<IMessage> _reader;
        private readonly ChannelWriter<IMessage> _writer;

        private readonly SessionWrapper _session;
        internal IDispatchTarget DispatchTarget { get; set; }

        internal Subscription(
            SessionWrapper session,
            MessageTransformer transformer)
        {
            _session = session;
            _transformer = transformer;

            var opts = new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = true,
            };
            var channel = Channel.CreateUnbounded<IMessage>(opts);
            _reader = channel.Reader;
            _writer = channel.Writer;
        }

        internal void MessageHandler(object sender, MessageEventArgs e)
        {
            _writer.TryWrite(e.Message);
        }

        public IAsyncEnumerable<Message<T>> AsAsyncEnumerable(CancellationToken token = default)
            => _reader
            .Transform(Deserialise)
            .AsAsyncEnumerable(token);

        private Message<T> Deserialise(IMessage message)
        {
            var payload = _transformer.Transform<T>(message);
            return new Message<T>(message, payload, _session, _transformer);
        }

        public async ValueTask DisposeAsync()
        {
            _writer.TryComplete();
            await _session.UnsubscribeDispatchTargetAsync(DispatchTarget, CancellationToken.None);
        }
    }
}