using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Volatile sequence counter that is cache line padded.
    /// </summary>
    public class Sequence
    {
        private CacheLineStorageLong _sequence;

        /// <summary>
        /// Construct a new sequence with a initial value
        /// </summary>
        /// <param name="initialValue">initial value</param>
        public Sequence(long initialValue)
        {
            _sequence = new CacheLineStorageLong(initialValue);
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