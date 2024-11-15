using PooledAwait;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading.Tasks;

namespace SolaceSharp.Operations
{
    internal class RequestOperation : IOperation
    {
        private ISession _session;
        private IMessage _message;
        private Guid _id;
        private readonly TaskCompletionSource<IMessage> _source = new TaskCompletionSource<IMessage>();

        public RequestOperation(ISession session, IMessage message)
        {
            _session = session;
            _message = message;
            _id = (Guid)message.CorrelationKey;
        }

        public Guid Id => _id;

        public void Execute()
        {
            while (true)
            {
                var res = _session.SendRequest(_message, out var _, 0);
                if (res != ReturnCode.SOLCLIENT_OK && res != ReturnCode.SOLCLIENT_IN_PROGRESS)
                    _source.SetException(new UnexpectedResponseException(res.ToString()));
                return;
            }
        }

        public void HandleResponse(MessageEventArgs reply)
        {
            if (reply.Message.CorrelationId != Id.ToString())
                return;

            _source.SetResult(reply.Message);
        }

        public void HandleResponse(SessionEventArgs args)
        {
        }

        public void HandleException(Exception ex) => _source.SetException(ex);
        public Task<IMessage> AsTask() => _source.Task;
    }
}
