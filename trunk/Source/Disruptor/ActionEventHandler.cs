using System;

namespace Disruptor
{
    /// <summary>
    /// Generic implementation of <see cref="IEventHandler{T}"/> used when a <see cref="IEventProcessor"/> is created with <see cref="Action"/>s.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class ActionEventHandler<T> : IEventHandler<T>
    {
        private readonly Action<long, T> _onAvailable;
        private readonly Action _onEndOfBatch;

        internal ActionEventHandler(Action<long, T> onAvailable, Action onEndOfBatch)
        {
            if (onAvailable == null) throw new ArgumentNullException("onAvailable");

            _onAvailable = onAvailable;

            if (onEndOfBatch == null)
            {
                _onEndOfBatch = () => { };
            }
            else
            {
                _onEndOfBatch = onEndOfBatch;    
            }
        }

        public void OnAvailable(long sequence, T data)
        {
            _onAvailable(sequence, data);
        }

        public void OnEndOfBatch()
        {
            _onEndOfBatch();
        }
    }
}