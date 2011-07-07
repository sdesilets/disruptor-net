using System.Collections.Concurrent;
using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionQueueConsumer
    {
        private long _value;
        private readonly BlockingCollection<long> _queue;
        private readonly long _iterations;
        private volatile bool _done;

        public ValueAdditionQueueConsumer(BlockingCollection<long> queue, long iterations)
        {
            _queue = queue;
            _iterations = iterations;
        }

        public long Value
        {
            get { return _value; }
        }

        public void Reset()
        {
            _value = 0L;

            _done = false;
        }

        public bool Done
        {
            get
            {
                return _done;
            }
        }

        public void Run()
        {
            for(long i = 0; i<_iterations;i++)
            {
                var value = _queue.Take();
                _value += value;
            }
            _done = true;
        }
    }
}
