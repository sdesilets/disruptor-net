namespace Disruptor.PerfTests.Support
{
    public class ValueMutationEventHandlerValueType : IEventHandler<long>
    {
        private readonly Operation _operation;
        private long _value;
        private volatile bool _done;
        private readonly long _iterations;

        public ValueMutationEventHandlerValueType(Operation operation, long iterations)
        {
            _operation = operation;
            _iterations = iterations;
        }

        public long Value
        {
            get { return _value; }
        }

        public bool Done
        {
            get { return _done; }
        }

        public void OnAvailable(long sequence, long data)
        {
            _value = _operation.Op(_value, data);

            if (sequence == _iterations - 1)
            {
                _done = true;
            }
        }

        public void OnEndOfBatch()
        {
        }
    }
}