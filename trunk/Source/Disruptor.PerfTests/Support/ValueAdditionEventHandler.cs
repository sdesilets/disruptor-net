using Disruptor.MemoryLayout;

namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionEventHandler : IEventHandler<ValueEvent>
    {
        private readonly long _iterations;
        private PaddedLong _value;
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public ValueAdditionEventHandler(long iterations)
        {
            _iterations = iterations;
        }

        public long Value
        {
            get { return _value.Data; }
        }

        public void OnNext(long sequence, ValueEvent value, bool endOfBatch)
        {
            _value.Data += value.Value;
            if(sequence == _iterations - 1)
            {
                _done = true;
            }
        }
    }
}


