using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Cache line padded sequence counter.
    /// Can be used across threads without worrying about false sharing if a located adjacent to another counter in memory.
    /// </summary>
    public class Sequence
    {
        private CacheLineStorageVolatileLong _sequence;

        /// <summary>
        /// Construct a new sequence that can be tracked across threads.
        /// </summary>
        /// <param name="initialValue">initial value</param>
        public Sequence(long initialValue)
        {
            _sequence = new CacheLineStorageVolatileLong(initialValue);
        }

        /// <summary>
        /// Current sequence number
        /// </summary>
        public virtual long Value
        {
            get { return _sequence.Data; }
            set { _sequence.Data = value; }
        }
    }
}