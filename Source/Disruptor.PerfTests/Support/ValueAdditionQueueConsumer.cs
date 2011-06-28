using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionQueueConsumer
    {
        private volatile bool _running;
        private long _sequence;
        private long _value;
        private readonly BlockingCollection<long> _queue;

        public ValueAdditionQueueConsumer(BlockingCollection<long> queue)
        {
            _queue = queue;
        }

        public long Value
        {
            get { return _value; }
        }

        public void Reset()
        {
            _value = 0L;
            Sequence = -1L;
        }

        public long Sequence
        {
            get { return Thread.VolatileRead(ref _sequence); }
            private set { Thread.VolatileWrite(ref _sequence, value); }
        }

        public void Halt()
        {
            _running = false;
        }

        public void Run()
        {
            _running = true;
            while (_running)
            {                
                try
                {
                    var value = _queue.Take();
                    _value += value;
                    _sequence++;
                }
                catch (Exception)
                {
                    break;
                }
            }
            Sequence = _sequence; // publish
        }
    }
}
