﻿namespace Disruptor
{
    /// <summary>
    /// EntryConsumers waitFor <see cref="IEntry"/>s to become available for consumption from the <see cref="RingBuffer"/>
    /// </summary>
    public interface IConsumer : IRunnable
    {
        /// <summary>
        /// Get the sequence up to which this Consumer has consumed <see cref="IEntry"/>s
        /// Return the sequence of the last consumed <see cref="IEntry"/>
        /// </summary>
        long Sequence { get; }

        /// <summary>
        /// Signal that this Consumer should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="ConsumerBarrier.Alert()"/> to notify the thread to check status.
        /// </summary>
        void Halt();
    }
}