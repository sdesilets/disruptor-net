namespace Disruptor.PerfTests.Support
{
    public class FizzBuzzEventHandler : IEventHandler<FizzBuzzEvent>
    {
        private readonly FizzBuzzStep _fizzBuzzStep;
        private readonly long _iterations;
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public long FizzBuzzCounter { get; private set; }

        public FizzBuzzEventHandler(FizzBuzzStep fizzBuzzStep, long iterations)
        {
            _fizzBuzzStep = fizzBuzzStep;
            _iterations = iterations;
        }

        public void OnNext(long sequence, FizzBuzzEvent data, bool endOfBatch)
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
                        ++FizzBuzzCounter;
                    }
                    break;
            }
            if(sequence == _iterations - 1)
            {
                _done = true;
            }
        }
    }
}
