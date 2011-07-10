using System.Diagnostics;
using Disruptor.Collections;

namespace Disruptor.PerfTests.Support
{
    public class LatencyStepHandler:IBatchHandler<long>
    {
        private readonly FunctionStep _functionStep;
        private readonly Histogram _histogram;
        private readonly long _nanoTimeCost;
        private readonly double _ticksToNanos;

        public LatencyStepHandler(FunctionStep functionStep, Histogram histogram, long nanoTimeCost, double ticksToNanos)
        {
            _functionStep = functionStep;
            _histogram = histogram;
            _nanoTimeCost = nanoTimeCost;
            _ticksToNanos = ticksToNanos;
        }

        public void OnAvailable(long sequence, long data) 
        {
            switch (_functionStep)
            {
                case FunctionStep.One:
                case FunctionStep.Two:
                    break;
                case FunctionStep.Three:
                    var duration = (Stopwatch.GetTimestamp() - data) * _ticksToNanos;
                    duration /= 3;
                    duration -= _nanoTimeCost;
                    _histogram.AddObservation((long)duration);
                    break;
            }
        }

        public void OnEndOfBatch() 
        {
        }
    }
}