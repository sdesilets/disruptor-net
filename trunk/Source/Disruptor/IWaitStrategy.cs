using System;

namespace Disruptor
{
    /// <summary>
    /// Strategy employed for making <see cref="IConsumer"/>s wait on a <see cref="RingBuffer{T}"/>.
    /// </summary>
    public interface IWaitStrategy
    {
        /// <summary>
        /// Wait for the given sequence to be available for consumption in a <see cref="RingBuffer{T"/>
        /// </summary>
        /// <param name="consumers">consumers further back the chain that must advance first</param>
        /// <param name="ringBuffer">ringBuffer on which to wait.</param>
        /// <param name="barrier">barrier the consumer is waiting on.</param>
        /// <param name="sequence">sequence to be waited on.</param>
        /// <returns>the sequence that is available which may be greater than the requested sequence.</returns>
        /// <exception cref="AlertException">if the status of the Disruptor has changed.</exception>
        /// <exception cref="InterruptedException">if the thread is interrupted.</exception> TODO
        long WaitFor<T>(IConsumer[] consumers, RingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier, long sequence) where T:IEntry;

        /// <summary>
        /// Wait for the given sequence to be available for consumption in a <see cref="RingBuffer{T}"/> with a timeout specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumers">consumers further back the chain that must advance first</param>
        /// <param name="ringBuffer">ringBuffer on which to wait.</param>
        /// <param name="barrier">barrier the consumer is waiting on.</param>
        /// <param name="sequence">sequence to be waited on.</param>
        /// <param name="timeout">timeout value to abort after.</param>
        /// <returns>the sequence that is available which may be greater than the requested sequence.</returns>
        /// /// <exception cref="AlertException">if the status of the Disruptor has changed.</exception>
        /// <exception cref="InterruptedException">if the thread is interrupted.</exception> TODO
        long WaitFor<T>(IConsumer[] consumers, RingBuffer<T> ringBuffer, IConsumerBarrier<T> barrier, long sequence,
                        TimeSpan timeout) where T : IEntry;

        /// <summary>
        /// Signal those waiting that the <see cref="RingBuffer{T}"/> cursor has advanced.
        /// </summary>
        void SignalAll();
    }
}