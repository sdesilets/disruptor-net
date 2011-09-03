using Disruptor.MemoryLayout;

namespace Disruptor.PerfTests.Support
{
    public class ValueMutationEventHandler : IEventHandler<ValueEvent>
    {
        private readonly Operation _operation;
        private PaddedLong _value;
        private volatile bool _done;
        private readonly long _iterations;

        public ValueMutationEventHandler(Operation operation, long iterations)
        {
            _operation = operation;
            _iterations = iterations;
        }

        public bool Done
        {
            get { return _done; }
        }

        public void OnNext(long sequence, ValueEvent data, bool endOfBatch)
        {
            _value.Data = _operation.Op(_value.Data, data.Value);

            if (sequence == _iterations - 1)
            {
                _done = true;
            }
        }
    }
}
