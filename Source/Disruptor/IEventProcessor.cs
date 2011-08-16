namespace Disruptor
{
    /// <summary>
    /// <see cref="IEventProcessor"/> waitFor events to become available for consumption from the <see cref="RingBuffer{T}"/>
    /// </summary>
    public interface IEventProcessor
    {
        /// <summary>
        /// Get the sequence up to which this <see cref="IEventProcessor"/> has consumed events
        /// Return the sequence of the last consumed event
        /// </summary>
        long Sequence { get; }

        /// <summary>
        /// Return true if the instance is started, false otherwise
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// Signal that this <see cref="IEventProcessor"/> should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="IDependencyBarrier{T}.Alert"/> to notify the thread to check status.
        /// </summary>
        void Halt();

        /// <summary>
        /// Starts this instance 
        /// </summary>
        void Run();

        /// <summary>
        /// Throttle the sequence publication to other threads
        /// Can only be applied to the last <see cref="IEventProcessor"/>s of a chain (the one tracked by the producer barrier)
        /// </summary>
        /// <param name="period">Sequence will be published every 'period' events</param>
        void DelaySequenceWrite(int period);
    }
}