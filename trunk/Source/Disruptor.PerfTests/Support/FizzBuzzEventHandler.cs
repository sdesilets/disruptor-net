namespace Disruptor.PerfTests.Support
{
    public class FizzBuzzEventHandler : IEventHandler<FizzBuzzEvent>
    {
        private readonly FizzBuzzStep _fizzBuzzStep;
        private readonly long _iterations;
        private long _fizzBuzzCounter;
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public long FizzBuzzCounter
        {
            get { return _fizzBuzzCounter; }
        }

        public FizzBuzzEventHandler(FizzBuzzStep fizzBuzzStep, long iterations)
        {
            _fizzBuzzStep = fizzBuzzStep;
            _iterations = iterations;
        }

        public void OnAvailable(long sequence, FizzBuzzEvent data)
        {
            switch (_fizzBuzzStep)
            {
                case FizzBuzzStep.Fizz:
                    data.Fizz = (data.Value%3) == 0;
                    break;
                case FizzBuzzStep.Buzz:
                    data.Buzz = (data.Value % 5) == 0;
                    break;

                case FizzBuzzStep.FizzBuzz:
                    if (data.Fizz && data.Buzz)
                    {
                        ++_fizzBuzzCounter;
                    }
                    break;
            }
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
