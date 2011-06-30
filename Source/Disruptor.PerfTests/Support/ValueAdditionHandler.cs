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

        public void OnCompletion()
        {
        }
    }
}


