namespace Disruptor
{
    /// <summary>
    /// <see cref="IEventProcessor"/> waitFor events to become available for consumption from the <see cref="RingBuffer{T}"/>
    /// </summary>
    public interface IEventProcessor
    {
        /// <summary>
        /// Return true if the instance is started, false otherwise
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// Return a reference to the <see cref="Sequence"/> up to which this instance has processed events
        /// </summary>
        Sequence Sequence { get; }

        /// <summary>
        /// Signal that this <see cref="IEventProcessor"/> should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="IDependencyBarrier.Alert"/> to notify the thread to check status.
        /// </summary>
        void Halt();

        /// <summary>
        /// Starts this instance 
        /// </summary>
        void Run();
    }
}