using System;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using SolaceSharp.Operations;

namespace SolaceSharp.Internal
{
    class OperationExecutor
    {
        private readonly Channel<IOperation> _channel;
        private readonly CancellationTokenSource _cts;
        private readonly Task _executeLoop;
        private int _disposed;
        public OperationExecutor()
        {
            //TODO: Make configurable
            _channel = Channel.CreateBounded<IOperation>(new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false, // always should be in async loop.
                SingleWriter = false,
                SingleReader = true,
            });
            _cts = new CancellationTokenSource();
            _executeLoop = Task.Run(ExecuteLoopAsync);
        }

        public ValueTask Post(IOperation operation)
        {
            return _channel.Writer.WriteAsync(operation);
        }

        private async Task ExecuteLoopAsync()
        {
            var reader = _channel.Reader;
            try
            {
                while (await reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var operation))
                    {
                        if (_cts.IsCancellationRequested)
                            continue;

                        try
                        {
                            operation.Execute();
                        }
                        catch (Exception ex)
                        {
                            operation.HandleException(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("Operation executing consumer completing.");
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Increment(ref _disposed) == 1)
            {
                _cts.Cancel();
                await _executeLoop.ConfigureAwait(false); // wait for drain writer
            }
        }
    }
}
