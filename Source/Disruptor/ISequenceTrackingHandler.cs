namespace Disruptor
{
    /// <summary>
    /// Used by the <see cref="BatchConsumer{T}"/> to set a callback allowing the <see cref="IBatchHandler{T}"/> to notify
    /// when it has finished consuming an <see cref="Entry{T}"/> if this happens after the <see cref="IBatchHandler{T}.OnAvailable"/> call.
    /// 
    /// Typically this would be used when the handler is performing some sort of batching operation such are writing to an IO device.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface ISequenceTrackingHandler<T> : IBatchHandler<T>
    {
        /// <summary>
        /// Call by the <see cref="BatchConsumer{T}"/> to setup the callback.
        /// </summary>
        /// <param name="sequenceTrackerCallback">callback on which to notify the <see cref="BatchConsumer{T}"/> that the sequence has progressed.</param>
        void SetSequenceTrackerCallback(BatchConsumer<T>.SequenceTrackerCallback sequenceTrackerCallback);
    }
}