using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3StepLatency
{
    [TestFixture]
    public sealed class Pipeline3StepLatencyBlockingCollectionPerfTest:AbstractPipeline3StepLatencyPerfTest
    {
        private readonly BlockingCollection<long> _stepOneQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepTwoQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepThreeQueue = new BlockingCollection<long>(Size);

        private readonly LatencyStepQueueConsumer _stepOneQueueConsumer;
        private readonly LatencyStepQueueConsumer _stepTwoQueueConsumer;
        private readonly LatencyStepQueueConsumer _stepThreeQueueConsumer;

        public Pipeline3StepLatencyBlockingCollectionPerfTest()
        {
            _stepOneQueueConsumer = new LatencyStepQueueConsumer(FunctionStep.One, _stepOneQueue, _stepTwoQueue, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepTwoQueueConsumer = new LatencyStepQueueConsumer(FunctionStep.Two, _stepTwoQueue, _stepThreeQueue, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepThreeQueueConsumer = new LatencyStepQueueConsumer(FunctionStep.Three, _stepThreeQueue, null, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
        }

        public override void RunPass()
        {
            _stepThreeQueueConsumer.Reset();

            new Thread(_stepOneQueueConsumer.Run) { Name = "Step 1 queues" }.Start();
            new Thread(_stepTwoQueueConsumer.Run) { Name = "Step 2 queues" }.Start();
            new Thread(_stepThreeQueueConsumer.Run) { Name = "Step 3 queues" }.Start();

            for (long i = 0; i < Iterations; i++)
            {
                _stepOneQueue.Add(Stopwatch.GetTimestamp());

                var pauseStart = Stopwatch.GetTimestamp();
                while (PauseNanos > (Stopwatch.GetTimestamp() - pauseStart) * TicksToNanos)
                {
                    // busy spin
                }
            }

            while (!_stepThreeQueueConsumer.Done)
            {
                // busy spin
            }
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}