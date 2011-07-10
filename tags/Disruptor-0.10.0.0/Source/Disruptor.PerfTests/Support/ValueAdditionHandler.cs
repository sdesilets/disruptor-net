namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionHandler:IBatchHandler<long>
    {
        private long _value;

        public long Value
        {
            get { return _value; }
        }

        public void Reset()
        {
            _value = 0;
        }

        public void OnAvailable(long sequence, long value)
        {
            _value += value;
        }

        public void OnEndOfBatch()
        {
        }
    }

    public class ValueAdditionHandler2 : IBatchHandler<ValueEntry>
    {
        private readonly ValueEntry _value = new ValueEntry();

        public ValueEntry Value
        {
            get { return _value; }
        }

        public void Reset()
        {
            _value.Value = 0;
        }

        public void OnAvailable(long sequence, ValueEntry value)
        {
            _value.Value += value.Value;
        }

        public void OnEndOfBatch()
        {
        }
    }
}


