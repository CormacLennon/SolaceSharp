using SolaceSystems.Solclient.Messaging;
using System.Threading.Tasks;
using System.Threading;
using SolaceSharp.Serialisation;
using SolaceSharp.Internal;

namespace SolaceSharp
{
    public class Message<T>
    {
        private readonly IMessage _message;
        private readonly SessionWrapper _session;
        private readonly MessageTransformer _transformer;

        internal Message(
            IMessage original,
            T message,
            SessionWrapper session,
            MessageTransformer transformer)
        {
            _message = original;
            Payload = message;
            _session = session;
            _transformer = transformer;
        }

        public T Payload { get; }

        public async ValueTask SendReply<TOut>(TOut reply, CancellationToken token = default)
        {
            var meta = new PublishMeta(_message.Destination.ToString(), false, true);
            var msg = _transformer.Transform(reply, meta);
            try
            {
                await _session.SendReply(msg, _message, token);
            }
            finally
            {
                _transformer.Return(msg);
            }
        }
    }
}
