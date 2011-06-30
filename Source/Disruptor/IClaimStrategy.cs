namespace Disruptor
{
    /// <summary>
    /// Strategies employed for claiming the sequence of <see cref="Entry{T}"/>s in the <see cref="RingBuffer{T}"/> by producers.
    /// </summary>
    public interface IClaimStrategy
    {
        /// <summary>
        /// Claim the next sequence index in the <see cref="RingBuffer{T}"/> and increment.
        /// </summary>
        /// <returns>The <see cref="Entry{T}"/> index to be used for the producer.</returns>
        long GetAndIncrement();

        /// <summary>
        /// Set the current sequence value for claiming <see cref="Entry{T}"/> in the <see cref="RingBuffer{T}"/>
        /// </summary>
        /// <param name="sequence">sequence to be set as the current value.</param>
        void SetSequence(long sequence);

        /// <summary>
        /// Wait for the current commit to reach a given sequence.
        /// </summary>
        /// <param name="sequence">sequence to wait for</param>
        /// <param name="ringBuffer">ringBuffer on which to wait forCursor</param>
        void WaitForCursor(long sequence, ISequencable ringBuffer);
    }
}
