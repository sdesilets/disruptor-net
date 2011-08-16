namespace Disruptor
{
    /// <summary>
    /// dependencyBarrier handed out for gating eventProcessors of the RingBuffer and dependent <see cref="IEventProcessor"/>(s)
    /// </summary>
    internal sealed class DependencyBarrier : IDependencyBarrier
    {
        private readonly IWaitStrategy _waitStrategy;
        private readonly Sequence _cursorSequence;
        private readonly Sequence[] _dependantProcessorSequences;
        private volatile bool _alerted;

        public DependencyBarrier(IWaitStrategy waitStrategy, Sequence cursorSequence, Sequence[] dependantProcessorSequences)
        {
            _waitStrategy = waitStrategy;
            _cursorSequence = cursorSequence;
            _dependantProcessorSequences = dependantProcessorSequences;
        }

        public WaitForResult WaitFor(long sequence)
        {
            return _waitStrategy.WaitFor(_dependantProcessorSequences, _cursorSequence, this, sequence);
        }

        public long Cursor
        {
            get { return _cursorSequence.Value; }
        }

        public bool IsAlerted
        {
            get { return _alerted; }
        }

        public void Alert()
        {
            _alerted = true;
            _waitStrategy.SignalAll();
        }

        public void ClearAlert()
        {
            _alerted = false;
        }
    }
}