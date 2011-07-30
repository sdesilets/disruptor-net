namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionHandlerValueType : IBatchHandler<long>
    {
        private readonly long _iterations;
        private readonly ValueEntry _value = new ValueEntry();
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public ValueAdditionHandlerValueType(long iterations)
        {
            _iterations = iterations;
        }

        public ValueEntry Value
        {
            get { return _value; }
        }

        public void Reset()
        {
            _value.Value = 0;
        }

        public void OnAvailable(long sequence, long value)
        {
            _value.Value += value;
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