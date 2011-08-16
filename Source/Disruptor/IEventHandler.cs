namespace Disruptor
{
    /// <summary>
    /// Callback interface to be implemented for processing <see cref="Event{T}"/>s as they become available in the <see cref="RingBuffer{T}"/>
    /// </summary>
    /// <typeparam name="T">Data stored in the <see cref="Event{T}"/> for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IEventHandler<in T>
    {
        /// <summary>
        /// Called when a publisher has committed an <see cref="Event{T}"/> to the <see cref="RingBuffer{T}"/>
        /// </summary>
        /// <param name="sequence">Sequence number committed to the <see cref="RingBuffer{T}"/></param>
        /// <param name="data">Data committed to the <see cref="RingBuffer{T}"/></param>
        /// <param name="endOfBatch">flag to indicate if this is the last event in a batch from the <see cref="RingBuffer{T}"/></param>
        void OnNext(long sequence, T data, bool endOfBatch);
    }
}