using PooledAwait;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SolaceSharp.Operations
{
    internal class ConnectOperation : IOperation
    {
        private PooledValueTaskSource _source;
        private ISession _session;

        public Guid Id => Guid.Empty;

        public ConnectOperation(ISession session, CancellationToken cancellationToken)
        {
            _source = PooledValueTaskSource.Create();
            _session = session;
        }

        public void Execute()
        {
            var res = _session.Connect();
            if (res != ReturnCode.SOLCLIENT_OK && res != ReturnCode.SOLCLIENT_IN_PROGRESS)
            {
                _source.SetException(new ConnectionFailedException(res.ToString()));
            }
        }

        public void HandleResponse(SessionEventArgs args)
        {
            if (args.Event == SessionEvent.UpNotice)
            {
                _source.SetResult();
                return;
            }

            switch (args.Event)
            {
                case SessionEvent.ConnectFailedError:
                    _source.SetException(new ConnectionFailedException(args.Info));
                    break;
                default:
                    _source.SetException(new UnexpectedResponseException(args.Event.ToString()));
                    break;
            }
        }

        public void HandleException(Exception ex) => _source.SetException(ex);

        public ValueTask AsValueTask() => _source.Task;
    }
}
