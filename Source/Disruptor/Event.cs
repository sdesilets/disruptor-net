namespace Disruptor
{
    /// <summary>
    /// Events are the items exchanged via a RingBuffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Event<T>
    {
        /// <summary>
        /// get: Get the sequence number assigned to this item in the series.
        /// set: Explicitly set the sequence number for this Event
        /// </summary>
        public long Sequence { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Initialise a new instance of <see cref="Event{T}"/> with a sequence number and the underlying data
        /// </summary>
        /// <param name="sequence">sequence number.</param>
        /// <param name="data">underlying data.</param>
        public Event(long sequence, T data) : this()
        {
            Sequence = sequence;
            Data = data;
        }
    }
}