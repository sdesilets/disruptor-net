using System.Diagnostics;
using Disruptor.Collections;

namespace Disruptor.PerfTests.Support
{
    public class LatencyStepEventHandler:IEventHandler<ValueEvent>
    {
        private readonly FunctionStep _functionStep;
        private readonly Histogram _histogram;
        private readonly long _nanoTimeCost;
        private readonly double _ticksToNanos;
        private readonly long _iterations;
        private volatile bool _done;

        public bool Done
        {
            get { return _done; }
        }

        public LatencyStepEventHandler(FunctionStep functionStep, Histogram histogram, long nanoTimeCost, double ticksToNanos, long iterations)
        {
            _functionStep = functionStep;
            _histogram = histogram;
            _nanoTimeCost = nanoTimeCost;
            _ticksToNanos = ticksToNanos;
            _iterations = iterations;
        }

        public void OnNext(long sequence, ValueEvent data, bool endOfBatch) 
        {
            switch (_functionStep)
            {
                case FunctionStep.One:
                case FunctionStep.Two:
                    break;
                case FunctionStep.Three:
                    var duration = (Stopwatch.GetTimestamp() - data.Value) * _ticksToNanos;
                    duration /= 3;
                    duration -= _nanoTimeCost;
                    _histogram.AddObservation((long)duration);
                    break;
            }
            if(sequence == _iterations - 1)
            {
                _done = true;
            }
        }
    }
}