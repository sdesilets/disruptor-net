namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionEventHandler : IEventHandler<ValueEvent>
    {
        private readonly long _iterations;
        private readonly ValueEvent _value = new ValueEvent();
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public ValueAdditionEventHandler(long iterations)
        {
            _iterations = iterations;
        }

        public ValueEvent Value
        {
            get { return _value; }
        }

        public void Reset()
        {
            _value.Value = 0;
        }

        public void OnNext(long sequence, ValueEvent value, bool endOfBatch)
        {
            _value.Value += value.Value;
            if(sequence == _iterations - 1)
            {
                _done = true;
            }
        }
    }
}


