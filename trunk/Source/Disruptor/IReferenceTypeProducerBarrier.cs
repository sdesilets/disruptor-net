namespace Disruptor
{
    /// <summary>
    /// Abstraction for claiming entries in a <see cref="RingBuffer{T}"/> while tracking dependent <see cref="IConsumer"/>s.
    /// </summary>
    public interface IReferenceTypeProducerBarrier<T> where T : class
    {
        ///<summary>
        /// Claim the next sequence number and a pre-allocated instance of T for a producer on the <see cref="RingBuffer{T}"/>
        ///</summary>
        ///<param name="data">A pre-allocated instance of T to be reused by the producer, to prevent memory allocation. This instance needs to be flushed properly before commiting back to the <see cref="RingBuffer{T}"/></param>
        ///<returns>the claimed sequence.</returns>
        long NextEntry(out T data);

        /// <summary>
        /// Commit an entry back to the <see cref="RingBuffer{T}"/> to make it visible to <see cref="IConsumer"/>s
        /// </summary>
        /// <param name="sequence">sequence number to be committed back to the <see cref="RingBuffer{T}"/></param>
        void Commit(long sequence);

        /// <summary>
        /// Delegate a call to the <see cref="RingBuffer{T}.Cursor"/>
        /// </summary>
        long Cursor { get; }
    }
}