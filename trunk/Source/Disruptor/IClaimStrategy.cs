﻿namespace Disruptor
{
    /// <summary>
    /// Strategies employed for claiming the sequence of <see cref="Event{T}"/>s in the <see cref="RingBuffer{T}"/> by producers.
    /// </summary>
    public interface IClaimStrategy
    {
        /// <summary>
        /// Increment the sequence index in the <see cref="RingBuffer{T}"/> and return the new value
        /// </summary>
        /// <returns>The <see cref="Event{T}"/> index to be used for the producer.</returns>
        long IncrementAndGet();

        ///<summary>
        /// Ensure dependent <see cref="Sequence"/>s are in range without over taking them for the buffer size.
        ///</summary>
        ///<param name="sequence">sequence to check is in range</param>
        ///<param name="dependentSequences">sequences to be checked for range.</param>
        void EnsureProcessorsAreInRange(long sequence, Sequence[] dependentSequences);

        ///<summary>
        /// Increment by a delta and get the result.
        ///</summary>
        ///<param name="delta">delta to increment by.</param>
        ///<returns>the result after incrementing.</returns>
        long IncrementAndGet(int delta);

        ///<summary>
        /// Serialise publishing in sequence.
        ///</summary>
        ///<param name="cursor">cursor to serialise against.</param>
        ///<param name="sequence">sequence to be applied</param>
        ///<param name="batchSize">batchSize of the sequence.</param>
        void SerialisePublishing(Sequence cursor, long sequence, long batchSize);
    }
}
