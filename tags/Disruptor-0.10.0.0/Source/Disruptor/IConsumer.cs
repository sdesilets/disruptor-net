namespace Disruptor
{
    /// <summary>
    /// EntryConsumers waitFor entries to become available for consumption from the <see cref="RingBuffer{T}"/>
    /// </summary>
    public interface IConsumer
    {
        /// <summary>
        /// Get the sequence up to which this Consumer has consumed entries
        /// Return the sequence of the last consumed entry
        /// </summary>
        long Sequence { get; }

        /// <summary>
        /// Signal that this Consumer should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="IConsumerBarrier{T}.Alert"/> to notify the thread to check status.
        /// </summary>
        void Halt();

        /// <summary>
        /// Starts the consumer 
        /// </summary>
        void Run();
    }
}