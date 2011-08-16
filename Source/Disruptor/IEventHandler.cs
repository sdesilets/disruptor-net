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
        void OnAvailable(long sequence, T data);

        /// <summary>
        /// Called after each batch of events have been processed before the next waitFor call on a <see cref="IDependencyBarrier{T}"/>.
        /// This can be taken as a hint to do flush type operations before waiting once again on the <see cref="IDependencyBarrier{T}"/>.
        /// The user should not expect any pattern or frequency to the batch size.
        /// </summary>
        void OnEndOfBatch();
    }
}