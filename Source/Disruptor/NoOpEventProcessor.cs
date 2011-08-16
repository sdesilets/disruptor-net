namespace Disruptor
{
    /// <summary>
    /// No operation version of a <see cref="IEventProcessor"/> that simply tracks a <see cref="RingBuffer{T}"/>.
    ///  This is useful in tests or for pre-filling a <see cref="RingBuffer{T}"/> from a producer.
    /// </summary>
    public sealed class NoOpEventProcessor<T>:IEventProcessor where T : class
    {
        private volatile bool _running;
        private readonly RingBufferTrackingSequence _sequence;

        /// <summary>
        /// Construct a <see cref="IEventProcessor"/> that simply tracks a <see cref="RingBuffer{T}"/>.
        /// </summary>
        /// <param name="ringBuffer"></param>
        public NoOpEventProcessor(RingBuffer<T> ringBuffer)
        {
            _sequence = new RingBufferTrackingSequence(ringBuffer);
        }

        /// <summary>
        /// NoOp
        /// </summary>
        public void Run()
        {
            _running = true;
        }

        /// <summary>
        /// No op
        /// </summary>
        /// <param name="period"></param>
        public void DelaySequenceWrite(int period)
        {
            
        }

        /// <summary>
        /// Return true if the instance is started, false otherwise
        /// </summary>
        public bool Running
        {
            get { return _running; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Sequence Sequence
        {
            get { return _sequence; }
        }

        /// <summary>
        /// NoOp
        /// </summary>
        public void Halt()
        {
            _running = false;
        }

	    private sealed class RingBufferTrackingSequence : Sequence
	    {
	        private readonly RingBuffer<T> _ringBuffer;
	
	        public RingBufferTrackingSequence(RingBuffer<T> ringBuffer) : base(RingBufferConvention.InitialCursorValue)
	        {
	            _ringBuffer = ringBuffer;
	        }

            public override long Value
            {
                get { return _ringBuffer.Cursor; }
            } 
	    }
    }
}