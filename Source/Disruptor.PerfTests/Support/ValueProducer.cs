using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class ValueProducer
    {
        private readonly Barrier _barrier;
        private readonly IValueTypeProducerBarrier<long> _producerBarrier;
        private readonly long _iterations;

        public ValueProducer(Barrier barrier, IValueTypeProducerBarrier<long> producerBarrier, long iterations)
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
                _producerBarrier.Commit(i);
            }
        }
    }
}
