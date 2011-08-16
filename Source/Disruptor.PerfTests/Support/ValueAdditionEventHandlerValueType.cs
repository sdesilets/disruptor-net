namespace Disruptor.PerfTests.Support
{
    public class ValueAdditionEventHandlerValueType : IEventHandler<long>
    {
        private readonly long _iterations;
        private readonly ValueEvent _value = new ValueEvent();
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public ValueAdditionEventHandlerValueType(long iterations)
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