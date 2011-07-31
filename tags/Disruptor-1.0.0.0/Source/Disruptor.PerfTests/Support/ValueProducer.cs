using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class ValueProducer
    {
        private readonly Barrier _barrier;
        private readonly IProducerBarrier<ValueEntry> _producerBarrier;
        private readonly long _iterations;

        public ValueProducer(Barrier barrier, IProducerBarrier<ValueEntry> producerBarrier, long iterations)
        {
            _barrier = barrier;
            _producerBarrier = producerBarrier;
            _iterations = iterations;
        }

        public void Run()
        {
            _barrier.SignalAndWait();

            for (long i = 0; i < _iterations; i++)
            {
                ValueEntry data;
                var sequence = _producerBarrier.NextEntry(out data);
                data.Value = i;
                _producerBarrier.Commit(sequence);
            }
        }
    }
}
