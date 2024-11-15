using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading.Tasks;

namespace SolaceSharp.Operations
{
    internal class ReplyOperation : IOperation
    {
        private ISession _session;
        private IMessage _reply;
        private IMessage _request;
        private Guid _id;

        private readonly TaskCompletionSource<bool> _source = new TaskCompletionSource<bool>();

        public ReplyOperation(
            ISession session, 
            IMessage reply,
            IMessage request,
            bool fireAndForget)
        {

            _session = session;
            _reply = reply;
            _request = request;
            _id = (Guid)reply.CorrelationKey;
        }

        public Guid Id => _id;

        public void Execute()
        {
            var res = _session.SendReply(_request, _reply);
            if (res != ReturnCode.SOLCLIENT_OK && res != ReturnCode.SOLCLIENT_IN_PROGRESS)
                _source.SetException(new UnexpectedResponseException(res.ToString()));
                //Acks to reply messages dont have a CorrolationKey so we set now
                // this api is so crap yo
            _source.SetResult(true);
        }

        public void HandleResponse(SessionEventArgs args)
        {
            _source.SetResult(true);
        }

        public void HandleException(Exception ex) => _source.SetException(ex);

        public ValueTask AsValueTask() => new ValueTask(_source.Task);
    }
}

