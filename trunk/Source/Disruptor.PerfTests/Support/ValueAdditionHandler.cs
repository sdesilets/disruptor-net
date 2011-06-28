namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionHandler:IBatchHandler<ValueEntry>
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

        public void OnAvailable(ValueEntry entry)
        {
            _value += entry.Value;
        }

        public void OnEndOfBatch()
        {
        }

        public void OnCompletion()
        {
        }
    }
}


