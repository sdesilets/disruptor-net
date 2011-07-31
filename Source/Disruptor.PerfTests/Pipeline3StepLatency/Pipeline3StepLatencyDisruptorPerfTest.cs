using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3StepLatency
{
    [TestFixture]
    public sealed class Pipeline3StepLatencyDisruptorPerfTest : AbstractPipeline3StepLatencyPerfTest
    {
        private readonly RingBuffer<ValueEntry> _ringBuffer;

        private readonly LatencyStepHandler _stepOneFunctionHandler;
        private readonly LatencyStepHandler _stepTwoFunctionHandler;
        private readonly LatencyStepHandler _stepThreeFunctionHandler;

        public Pipeline3StepLatencyDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEntry>(()=>new ValueEntry(), Size,
                                   ClaimStrategyOption.SingleProducer,
                                   WaitStrategyOption.BusySpin);

            _stepOneFunctionHandler = new LatencyStepHandler(FunctionStep.One, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepTwoFunctionHandler = new LatencyStepHandler(FunctionStep.Two, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepThreeFunctionHandler = new LatencyStepHandler(FunctionStep.Three, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);

            _ringBuffer.ConsumeWith(_stepOneFunctionHandler)
                .Then(_stepTwoFunctionHandler)
                .Then(_stepThreeFunctionHandler);
        }

        public override void RunPass()
        {
            _ringBuffer.StartConsumers();

            for (long i = 0; i < Iterations; i++)
            {
                ValueEntry data;
                var sequence = _ringBuffer.NextEntry(out data);
                data.Value = Stopwatch.GetTimestamp();
                _ringBuffer.Commit(sequence);

                var pauseStart = Stopwatch.GetTimestamp();
                while (PauseNanos > (Stopwatch.GetTimestamp() - pauseStart) * TicksToNanos)
                {
                    // busy spin
                }
            }

            while (!_stepThreeFunctionHandler.Done)
            {
                // busy spin
            }

            _ringBuffer.Halt();
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}