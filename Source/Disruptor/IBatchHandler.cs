using System;

namespace Disruptor
{
    /// <summary>
    /// Callback interface to be implemented for processing <see cref="IEntry"/>s as they become available in the <see cref="RingBuffer"/>
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IBatchHandler<in T> where T:IEntry
    {
        /// <summary>
        /// Called when a publisher has committed an <see cref="IEntry"/> to the <see cref="RingBuffer"/>
        /// </summary>
        /// <param name="entry">Committed to the <see cref="RingBuffer"/></param>
        /// <exception cref="Exception">If the BatchHandler would like the exception handled further up the chain.</exception>
        void OnAvailable(T entry);

        /// <summary>
        /// Called after each batch of items has been have been processed before the next waitFor call on a {@link ConsumerBarrier}.
        /// This can be taken as a hint to do flush type operations before waiting once again on the {@link ConsumerBarrier}.
        /// The user should not expect any pattern or frequency to the batch size.
        /// </summary>
        /// <exception cref="Exception">If the BatchHandler would like the exception handled further up the chain.</exception>
        void OnEndOfBatch();

        /// <summary>
        /// Called when processing of <see cref="IEntry"/>s is complete for clean up.
        /// </summary>
        void OnCompletion();
    }
}