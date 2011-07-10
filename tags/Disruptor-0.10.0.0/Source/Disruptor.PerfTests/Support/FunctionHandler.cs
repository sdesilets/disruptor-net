namespace Disruptor.PerfTests.Support
{
    public class FunctionHandler:IBatchHandler<FunctionEntry>
    {
        private readonly FunctionStep _functionStep;
        private long _stepThreeCounter;

        public long StepThreeCounter
        {
            get { return _stepThreeCounter; }
        }

        public FunctionHandler(FunctionStep functionStep)
        {
            _functionStep = functionStep;
        }

        public void Reset()
        {
            _stepThreeCounter = 0L;
        }

        public void OnAvailable(long sequence, FunctionEntry data)
        {
            switch (_functionStep)
            {
                case FunctionStep.One:
                    data.StepOneResult = data.OperandOne + data.OperandTwo;
                    break;
                case FunctionStep.Two:
                    data.StepTwoResult = data.StepOneResult + 3L;
                    break;

                case FunctionStep.Three:
                    if ((data.StepTwoResult & 4L) == 4L)
                    {
                        _stepThreeCounter++;
                    }
                    break;
            }
        }

        public void OnEndOfBatch()
        {
        }
    }
}