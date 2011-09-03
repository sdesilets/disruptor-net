namespace Disruptor
{
    /// <summary>
    /// Strategy options which are available to those waiting on a <see cref="RingBuffer{T}"/>
    /// </summary>
    public enum WaitStrategyOption
    {
        /// <summary>
        /// This strategy uses a condition variable inside a lock to block the <see cref="IEventProcessor"/> which saves CPU resource as the expense of lock contention.
        /// </summary>
        Blocking,
        /// <summary>
        /// This strategy calls Thread.yield() in a loop as a waiting strategy which reduces contention at the expense of CPU resource.
        /// </summary>
        Yielding,
        /// <summary>
        /// This strategy call spins in a loop as a waiting strategy which is lowest and most consistent latency but ties up a CPU
        /// </summary>
        BusySpin,
        /// <summary>
        /// This strategy uses a progressive back off strategy by first spinning, then yielding, then sleeping for 1ms periods.
        /// This is a good strategy for burst traffic then quiet periods when latency is not critical.
        /// </summary>
        SpinWait
    }
}