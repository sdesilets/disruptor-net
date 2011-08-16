namespace Disruptor
{
    /// <summary>
    /// Strategy employed for making <see cref="IEventProcessor"/>s wait on a <see cref="RingBuffer{T}"/>.
    /// </summary>
    public interface IWaitStrategy
    {
        /// <summary>
        /// Wait for the given sequence to be available for consumption in a <see cref="RingBuffer{T}"/>
        /// </summary>
        /// <param name="dependents">dependents further back the chain that must advance first</param>
        /// <param name="ringBufferCursor">Ring buffer cursor on which to wait.</param>
        /// <param name="barrier">barrier the <see cref="IEventProcessor"/> is waiting on.</param>
        /// <param name="sequence">sequence to be waited on.</param>
        /// <returns>the sequence that is available which may be greater than the requested sequence.</returns>
        WaitForResult WaitFor(Sequence[] dependents, Sequence ringBufferCursor, IDependencyBarrier barrier, long sequence);

        /// <summary>
        /// Signal those waiting that the <see cref="RingBuffer{T}"/> cursor has advanced.
        /// </summary>
        void SignalAll();
    }
}