namespace Disruptor.PerfTests.Support
{
    public class FizzBuzzHandler : IBatchHandler<FizzBuzzEntry>
    {
        private readonly FizzBuzzStep _fizzBuzzStep;
        private long _fizzBuzzCounter;

        public long FizzBuzzCounter
        {
            get { return _fizzBuzzCounter; }
        }

        public FizzBuzzHandler(FizzBuzzStep fizzBuzzStep)
        {
            _fizzBuzzStep = fizzBuzzStep;
        }

        public void Reset()
        {
            _fizzBuzzCounter = 0L;
        }

        public void OnAvailable(long sequence, FizzBuzzEntry data)
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
        }

        public void OnEndOfBatch()
        {
        }
    }
}
