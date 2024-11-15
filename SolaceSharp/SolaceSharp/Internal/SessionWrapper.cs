using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using SolaceSharp.Operations;
using SolaceSharp.Serialisation;

namespace SolaceSharp.Internal
{
    internal class SessionWrapper : IAsyncDisposable
    {
        private ConnectOperation _connectOperation;
        private readonly OperationExecutor _executor;
        private readonly OperationTracker _tracker;

        public readonly ISession _session;

        public SessionWrapper(ISession session)
        {
            _session = session;
            _executor = new OperationExecutor();
            _tracker = new OperationTracker();
        }

        public ValueTask<ReturnCode> ClearStatsAsync()
        {
            var res = _session.ClearStats();
            return new ValueTask<ReturnCode>(res);
        }

        public async ValueTask ConnectAsync(CancellationToken token)
        {
            _connectOperation = new ConnectOperation(_session, token);
            await _executor.Post(_connectOperation);
            await _connectOperation.AsValueTask();
        }

        public async ValueTask SendAsync(IMessage message, bool fireAndForget, CancellationToken token)
        {
            var send = SendOperation.Create(_session, message, fireAndForget);
            using (var tracker = _tracker.TrackOperation(send, token))
            {
                await _executor.Post(send);
                await send.AsValueTask();
            }
        }

        public async ValueTask<PublishFuture> SendConcurrentAsync(IMessage message, CancellationToken token)
        {
            var send = SendOperation.Create(_session, message, false);
            var tracker = _tracker.TrackOperation(send, token);
            await _executor.Post(send);
            return new PublishFuture(send, message, tracker);
        }

        public Task SendAsync(IMessage[] messages)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<IMessage> SendRequest(IMessage message, TimeSpan? timeout = null, CancellationToken token = default)
        {
            var request = new RequestOperation(_session, message);
            using (var tracker = _tracker.TrackOperation(request, token, timeout))
            {
                await _executor.Post(request);
                return await request.AsTask();
            }
        }

        public async ValueTask SendReply(IMessage message, IMessage replyingTo, CancellationToken token = default)
        {
            var reply = new ReplyOperation(_session, message, replyingTo, false);
            using (var tracker = _tracker.TrackOperation(reply, token))
            {
                await _executor.Post(reply);
                await reply.AsValueTask();
            }
        }

        public async Task<ISubscription<T>> SubscribeAsync<T>(
            string topic,
            MessageTransformer transform,
            CancellationToken token = default)
        {
            var key = Guid.NewGuid();
            var sub = new SubscribeOperation<T>(topic, _session, this, key, transform, token);

            using (var tracker = _tracker.TrackOperation(sub, token))
            {
                await _executor.Post(sub);
                return await sub.AsTask();
            }
        }

        public async Task<bool> UnsubscribeDispatchTargetAsync(IDispatchTarget target, CancellationToken token = default)
        {
            var key = Guid.NewGuid();
            var sub = new UnsubscribeDispatchTargetOperation(target, _session, key, token);

            using (var tracker = _tracker.TrackOperation(sub, token))
            {
                await _executor.Post(sub);
                return await sub.AsTask();
            }
        }

        public void HandleMessageEvent(object s, MessageEventArgs msg)
        {
            _tracker.HandleMessageEvent(msg);
        }

        public void HandleSessionEvent(object s, SessionEventArgs args)
        {
            if (args.Event == SessionEvent.CanSend)
                return;

            if (args.Event == SessionEvent.UpNotice || args.Event == SessionEvent.ConnectFailedError)
            {
                Thread.CurrentThread.Name = "Solace";
                _connectOperation.HandleResponse(args);
                return;
            }
            _tracker.HandleSessionEvent(args);
        }

        public async ValueTask DisposeAsync()
        {
            await _executor.DisposeAsync();
            _tracker.Dispose();
        }
    }
}