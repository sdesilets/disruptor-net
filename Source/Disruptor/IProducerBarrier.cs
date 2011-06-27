namespace Disruptor
{
    /// <summary>
    /// Abstraction for claiming <see cref="IEntry"/>s in a <see cref="RingBuffer"/> while tracking dependent <see cref="IConsumer"/>s
    /// </summary>
    /// <typeparam name="T"><see cref="IEntry"/> implementation stored in the <see cref="RingBuffer"/></typeparam>
    public interface IProducerBarrier<T> where T:IEntry
    {
        /// <summary>
        /// Claim the next <see cref="IEntry"/> in sequence for a producer on the <see cref="RingBuffer"/>
        /// </summary>
        /// <returns>the claimed <see cref="IEntry"/></returns>
        T NextEntry();

        /// <summary>
        /// Commit an entry back to the <see cref="RingBuffer"/> to make it visible to <see cref="IConsumer"/>s
        /// </summary>
        /// <param name="entry">entry to be committed back to the <see cref="RingBuffer"/></param>
        void Commit(T entry);

        /// <summary>
        /// Delegate a call to the <see cref="RingBuffer.Cursor"/>
        /// </summary>
        long Cursor { get; }
    }
}