using PooledAwait;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading.Tasks;
using SolaceSharp.Operations;

namespace SolaceSharp
{
    public class PublishFuture : IDisposable
    {
        private bool _disposed;
        private readonly IMessage _message;
        private readonly IDisposable _cleanup;
        private readonly SendOperation _operation;

        internal PublishFuture(SendOperation operation, IMessage msg, IDisposable cleanup)
        {
            _message = msg;
            _cleanup = cleanup;
            _operation = operation;
        }

        public async ValueTask<SendResponse> GetResponseAsync()
        {
            try
            {
                return await _operation.AsValueTask();
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            Pool.Return(_message);
            _cleanup?.Dispose();
            _disposed = true;
        }
    }
}
