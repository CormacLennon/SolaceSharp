using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using PooledAwait;

namespace SolaceSharp.Internal
{
    abstract class ValueTaskSource<TResult> : IValueTaskSource<TResult>
    {
        protected ManualResetValueTaskSourceCore<TResult> _source;

        public virtual TResult GetResult(short token)
            => _source.GetResult(token);

        public ValueTaskSourceStatus GetStatus(short token)
            => _source.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _source.OnCompleted(continuation, state, token, flags);
        public short Version => _source.Version;

        public ValueTask<TResult> AsValueTask()
        {
            return new ValueTask<TResult>(this, _source.Version);
        }
    }

    internal abstract class PooledValueTaskSource<TSelf, TResult> : ValueTaskSource<TResult>, IResettable
        where TSelf : PooledValueTaskSource<TSelf, TResult>, new()
    {

        public static TSelf Create()
        {
            return Pool.TryRent<TSelf>() ?? new TSelf();
        }

        public override TResult GetResult(short token)
        {
            try
            {
                return base.GetResult(token);
            }
            finally
            {
                Pool.Return((TSelf)this);
            }
        }

        public abstract void Reset();

        public bool TryReset()
        {
            try
            {
                Reset();
            }
            finally
            {
                _source.Reset();
            }
            return true;
        }
    }
}
