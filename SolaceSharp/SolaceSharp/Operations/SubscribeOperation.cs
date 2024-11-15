using PooledAwait;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using SolaceSharp.Serialisation;
using SolaceSharp.Internal;

namespace SolaceSharp.Operations
{
    internal class SubscribeOperation<T> : IOperation
    {
        private readonly TaskCompletionSource<Subscription<T>> _source;
        private readonly ISession _session;

        private readonly string _topic;
        private readonly Guid _corrolationKey;
        private readonly Subscription<T> _sub;

        private IDispatchTarget _target;

        public Guid Id => _corrolationKey;

        public SubscribeOperation(
            string topic,
            ISession session,
            SessionWrapper sessionWrapper,
            Guid corrolationKey,
            MessageTransformer transformer,
            CancellationToken cancellationToken)
        {
            _topic = topic;
            _corrolationKey = corrolationKey;
            _source = new TaskCompletionSource<Subscription<T>>();
            _session = session;
            _sub = new Subscription<T>(sessionWrapper, transformer);
        }

        public void Execute()
        {
            var properties = new FlowProperties();
            var topic = ContextFactory.Instance.CreateTopic(_topic);
            _target = _session.CreateDispatchTarget(ContextFactory.Instance.CreateTopic(_topic), _sub.MessageHandler);
            var res = _session.Subscribe(_target, SubscribeFlag.RequestConfirm, _corrolationKey);

            if (res != ReturnCode.SOLCLIENT_IN_PROGRESS)
                _source.SetException(new UnexpectedResponseException(res.ToString()));
        }

        public void HandleException(Exception ex) => _source.SetException(ex);

        public void HandleResponse(SessionEventArgs args)
        {
            switch (args.Event)
            {
                case SessionEvent.SubscriptionOk:
                    _sub.DispatchTarget = _target;
                    _source.SetResult(_sub);
                    break;
                case SessionEvent.SubscriptionError:
                    _source.SetException(new SubscribeFailedException());
                    break;
                default:
                    _source.SetException(new UnexpectedResponseException(args.Event.ToString()));
                    break;
            }
        }

        public Task<Subscription<T>> AsTask() => _source.Task;
    }
}
