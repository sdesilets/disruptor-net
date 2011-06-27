namespace Disruptor
{
    /// <summary>
    /// No operation version of a <see cref="IConsumer"/> that simply tracks a <see cref="RingBuffer{T}"/>.
    ///  This is useful in tests or for pre-filling a <see cref="RingBuffer{T}"/> from a producer.
    /// </summary>
    public sealed class NoOpConsumer<T>:IConsumer where T:IEntry
    {
        private readonly RingBuffer<T> _ringBuffer;

        /// <summary>
        /// Construct a <see cref="IConsumer"/> that simply tracks a <see cref="RingBuffer{T}"/>.
        /// </summary>
        /// <param name="ringBuffer"></param>
        public NoOpConsumer(RingBuffer<T> ringBuffer)
        {
            _ringBuffer = ringBuffer;
        }

        public void Run()
        {
        }

        public long Sequence
        {
            get { return _ringBuffer.Cursor; }
        }

        public void Halt()
        {
        }
    }
}