namespace Disruptor
{
    /// <summary>
    /// Abstraction for claiming entries in a <see cref="RingBuffer{T}"/> while tracking dependent <see cref="IConsumer"/>s.
    /// This barrier can be used to pre-fill a <see cref="RingBuffer{T}"/> but only when no other producers are active.
    /// </summary>
    /// <typeparam name="T"><see cref="Entry{T}"/> implementation stored in the <see cref="RingBuffer{T}"/></typeparam>
    public interface IValueTypeForceFillProducerBarrier<in T> where T:struct
    {
        /// <summary>
        /// Claim a specific sequence in the <see cref="RingBuffer{T}"/> when only one producer is involved.
        /// </summary>
        /// <param name="sequence">sequence to be claimed.</param>
        void ClaimEntry(long sequence);

        /// <summary>
        /// Commit an entry back to the <see cref="RingBuffer{T}"/> to make it visible to <see cref="IConsumer"/>s.
        /// Only use this method when forcing a sequence and you are sure only one producer exists.
        /// This will cause the <see cref="RingBuffer{T}"/> to advance the <see cref="RingBuffer{T}.Cursor"/> to this sequence.
        /// </summary>
        /// <param name="sequence">sequence number to be committed back to the <see cref="RingBuffer{T}"/></param>
        /// <param name="data">data to be committed back to the <see cref="RingBuffer{T}"/></param>
        void Commit(long sequence, T data);

        /// <summary>
        /// Delegate a call to the <see cref="RingBuffer{T}.Cursor"/>
        /// </summary>
        /// <returns>value of the cursor for entries that have been published.</returns>
        long Cursor { get; }
    }
}