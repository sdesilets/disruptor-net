namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionHandler : IBatchHandler<ValueEntry>
    {
        private readonly long _iterations;
        private readonly ValueEntry _value = new ValueEntry();
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public ValueAdditionHandler(long iterations)
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

        public void OnAvailable(long sequence, ValueEntry value)
        {
            _value.Value += value.Value;
            if(sequence == _iterations - 1)
            {
                _done = true;
            }
        }

        public void OnEndOfBatch()
        {
        }
    }
}


