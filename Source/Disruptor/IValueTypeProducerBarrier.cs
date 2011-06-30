namespace Disruptor
{
    /// <summary>
    /// Abstraction for claiming entries in a <see cref="ValueTypeRingBuffer{T}"/> while tracking dependent <see cref="IConsumer"/>s.
    /// </summary>
    public interface IValueTypeProducerBarrier<in T> where T : struct
    {
        /// <summary>
        /// Commit an entry back to the <see cref="ValueTypeRingBuffer{T}"/> to make it visible to <see cref="IConsumer"/>s
        /// </summary>
        /// <param name="data"></param>
        void Commit(T data);

        /// <summary>
        /// Delegate a call to the <see cref="ValueTypeRingBuffer{T}.Cursor"/>
        /// </summary>
        long Cursor { get; }
    }
}

