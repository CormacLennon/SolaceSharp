using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SolaceSharp.Operations
{
    internal class UnsubscribeDispatchTargetOperation : IOperation
    {
        private readonly TaskCompletionSource<bool> _source;
        private readonly ISession _session;
        private readonly IDispatchTarget _target;
        private readonly Guid _corrolationKey;

        public Guid Id => _corrolationKey;

        public UnsubscribeDispatchTargetOperation(
            IDispatchTarget topic,
            ISession session,
            Guid corrolationKey,
            CancellationToken cancellationToken)
        {
            _target = topic;
            _corrolationKey = corrolationKey;
            _source = new TaskCompletionSource<bool>();
            _session = session;
        }

        public void Execute()
        {
            var res = _session.Unsubscribe(_target, SubscribeFlag.RequestConfirm, _corrolationKey);
            if (res != ReturnCode.SOLCLIENT_IN_PROGRESS)
                _source.SetException(new UnexpectedResponseException(res.ToString()));
        }

        public void HandleException(Exception ex) => _source.SetException(ex);

        public void HandleResponse(SessionEventArgs args)
        {
            switch (args.Event)
            {
                case SessionEvent.SubscriptionOk:
                    _source.SetResult(true);
                    break;
                case SessionEvent.SubscriptionError:
                    _source.SetException(new SubscribeFailedException());
                    break;
                default:
                    _source.SetException(new UnexpectedResponseException(args.Event.ToString()));
                    break;
            }
        }

        public Task<bool> AsTask() => _source.Task;
    }
}
