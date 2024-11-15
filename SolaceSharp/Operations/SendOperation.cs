using SolaceSharp.Internal;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading;
using IResettable = PooledAwait.IResettable;

namespace SolaceSharp.Operations
{
    public enum SendResponse
    {
        Ack = 0,

        RejectedMessageError = 1,

        MessageTooBigError = 2,

        AssuredDeliveryDown = 3,

        Unknown = 4,

        Pending = 5,
    }

    internal class SendOperation : PooledValueTaskSource<SendOperation, SendResponse>, IOperation, IResettable
    {
        private ISession _session;
        private IMessage _message;
        private bool _fireAndforget;
        private Guid _id;

        public static SendOperation Create(ISession session, IMessage message, bool fireAndForget)
        {
            var send = Create();
            send._session = session;
            send._message = message;
            send._fireAndforget = fireAndForget;
            send._id = (Guid)message.CorrelationKey;
            return send;
        }

        public Guid Id => _id;

        public void Execute()
        {
            while (true)
            {
                var res = _session.Send(_message);
                if (res != ReturnCode.SOLCLIENT_OK)
                {
                    Thread.Sleep(1);
                    continue;
                }
                if (_fireAndforget)
                    _source.SetResult(SendResponse.Ack);
                return;
            }
        }

        public void HandleResponse(SessionEventArgs args)
        {
            if (_fireAndforget)
                return;

            _source.SetResult(GetSendResponse(args.Event));
        }

        public void HandleException(Exception ex) => _source.SetException(ex);

        private SendResponse GetSendResponse(SessionEvent e)
        {
            switch (e)
            {
                case SessionEvent.Acknowledgement:
                    return SendResponse.Ack;
                case SessionEvent.MessageTooBigError:
                    return SendResponse.MessageTooBigError;
                case SessionEvent.RejectedMessageError:
                    return SendResponse.RejectedMessageError;
                case SessionEvent.AssuredDeliveryDown:
                    return SendResponse.AssuredDeliveryDown;
                default:
                    return SendResponse.Unknown;
            }
        }

        public override void Reset()
        {
            _session = null;
            _message = null;
            _source = default;
            _fireAndforget = false;
        }
    }
}
