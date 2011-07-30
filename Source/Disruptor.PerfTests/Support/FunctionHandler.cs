using System;

namespace Disruptor.PerfTests.Support
{
    public class FunctionHandler:IBatchHandler<FunctionEntry>
    {
        private readonly FunctionStep _functionStep;
        private long _stepThreeCounter;
        private readonly long _iterations;
        private volatile bool _done;

        public long StepThreeCounter
        {
            get { return _stepThreeCounter; }
        }

        public bool Done
        {
            get { return _done; }
        }

        public FunctionHandler(FunctionStep functionStep, long iterations)
        {
            _functionStep = functionStep;
            _iterations = iterations;
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

            if(sequence == _iterations-1)
            {
                _done = true;
            }
        }

        public void OnEndOfBatch()
        {
        }
    }
}