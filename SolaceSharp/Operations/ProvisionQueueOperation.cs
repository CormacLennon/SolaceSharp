using SolaceSharp.Internal;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolaceSharp.Operations
{
    internal class ProvisionQueueOperation : IOperation
    {
        private readonly ISession _session;
        private readonly string _queue;
        private readonly Guid _corrolationKey;
        private readonly TaskCompletionSource<bool> _source;

        public ProvisionQueueOperation(
            ISession session,
            string queue,
            SessionWrapper sessionWrapper,
            Guid corrolationKey)
        {
            _queue = queue;
            _corrolationKey = corrolationKey;
            _source = new TaskCompletionSource<bool>();
            _session = session;
        }

        public Guid Id => _corrolationKey;

        public void Execute()
        {
            var props = new EndpointProperties()
            {
                MaxMsgSize = 1024 * 1024,
                Permission = EndpointProperties.EndpointPermission.Consume,
                AccessType = EndpointProperties.EndpointAccessType.NonExclusive,
            };

            var q = ContextFactory.Instance.CreateQueue(_queue);
            var res = _session.Provision(q, props, ProvisionFlag.IgnoreErrorIfEndpointDoesNotExist, _corrolationKey);

            if (res != ReturnCode.SOLCLIENT_OK && res != ReturnCode.SOLCLIENT_IN_PROGRESS)
                _source.SetException(new UnexpectedResponseException(res.ToString()));
        }

        public void HandleException(Exception ex) => _source.SetException(ex);

        public void HandleResponse(SessionEventArgs args)
        {
            switch (args.Event)
            {
                case SessionEvent.ProvisionOk:
                    _source.SetResult(true);
                    break;
                case SessionEvent.ProvisionError:
                    _source.SetException(new QueueProvisionFailure());
                    break;
                default:
                    _source.SetException(new UnexpectedResponseException(args.Event.ToString()));
                    break;
            }
        }
    }
}


class Queue : IAsyncDisposable
{
    public string Name { get; set; }
    public bool Durable { get; set; }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}