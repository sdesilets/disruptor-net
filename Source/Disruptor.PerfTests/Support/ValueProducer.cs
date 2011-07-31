using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class ValueProducer
    {
        private readonly Barrier _barrier;
        private readonly long _iterations;
        private readonly RingBuffer<ValueEntry> _ringBuffer;

        public ValueProducer(Barrier barrier, RingBuffer<ValueEntry> ringBuffer, long iterations)
        {
            _barrier = barrier;
            _ringBuffer = ringBuffer;
            _iterations = iterations;
        }

        public void Run()
        {
            _barrier.SignalAndWait();

            for (long i = 0; i < _iterations; i++)
            {
                ValueEntry data;
                var sequence = _ringBuffer.NextEntry(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }
        }
    }
}
