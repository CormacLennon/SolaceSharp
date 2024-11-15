using SolaceSharp.Operations;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

namespace SolaceSharp.Internal
{
    internal class OperationTracker : IDisposable
    {
        private readonly ConcurrentDictionary<object, IOperation> _operations = new ConcurrentDictionary<object, IOperation>();

        //TODO: make this less alloc-y
        public IDisposable TrackOperation(IOperation operation, CancellationToken token, TimeSpan? timeout = null)
        {
            _operations[operation.Id] = operation;
            if (timeout is null)
            {
                // We need a default timeout for everything as solace may not ack
                timeout = TimeSpan.FromSeconds(30);
            }

            var timeoutSource = new CancellationTokenSource();
            timeoutSource.CancelAfter(timeout.Value);

            var timeoutToken = timeoutSource.Token;
            timeoutToken.Register(() => operation.HandleException(new TimeoutException($"Request timed out.")));

            var cts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutToken);

            cts.Token.Register(() => CleanUp(operation.Id));

            return new CompositeDisposable(cts, timeoutSource, Disposable.Create(() => CleanUp(operation.Id)));
        }

        public void HandleSessionEvent(SessionEventArgs args)
        {
            if (args.CorrelationKey is null)
                return;

            if (_operations.TryRemove(args.CorrelationKey, out var operation))
            {
                operation.HandleResponse(args);
            }
        }

        public void HandleMessageEvent(MessageEventArgs args)
        {
            if (!args.Message.IsReplyMessage)
                return;
            var guid = Guid.Parse(args.Message.CorrelationId);
            if (_operations.TryRemove(guid, out var operation))
            {
                var ro = operation as RequestOperation;
                if (ro == null)
                    return;
                ro.HandleResponse(args);
            }
        }

        private void CleanUp(Guid key)
        {
            _operations.TryRemove(key, out var operation);
        }

        public void Dispose()
        {
            _operations.Clear();
        }
    }
}
