using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class ValueProducer
    {
        private readonly Barrier _barrier;
        private readonly long _iterations;
        private readonly RingBuffer<ValueEvent> _ringBuffer;

        public ValueProducer(Barrier barrier, RingBuffer<ValueEvent> ringBuffer, long iterations)
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
                ValueEvent data;
                var sequence = _ringBuffer.NextEvent(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }
        }
    }
}
