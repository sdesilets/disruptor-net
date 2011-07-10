namespace Disruptor.PerfTests.Support
{
    public class ValueMutationHandler : IBatchHandler<long>
    {
        private readonly Operation _operation;
        private long _value;

        public ValueMutationHandler(Operation operation)
        {
            _operation = operation;
        }

        public void Reset()
        {
            _value = 0L;
        }

        public long Value
        {
            get { return _value; }
        }

        public void OnAvailable(long sequence, long data)
        {
            _value = _operation.Op(_value, data);
        }

        public void OnEndOfBatch()
        {
        }
    }
}
