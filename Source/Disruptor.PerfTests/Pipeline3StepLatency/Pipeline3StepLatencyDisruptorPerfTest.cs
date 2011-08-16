using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3StepLatency
{
    [TestFixture]
    public sealed class Pipeline3StepLatencyDisruptorPerfTest : AbstractPipeline3StepLatencyPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;

        private readonly LatencyStepEventHandler _stepOneFunctionEventHandler;
        private readonly LatencyStepEventHandler _stepTwoFunctionEventHandler;
        private readonly LatencyStepEventHandler _stepThreeFunctionEventHandler;

        public Pipeline3StepLatencyDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEvent>(()=>new ValueEvent(), Size,
                                   ClaimStrategyOption.SingleProducer,
                                   WaitStrategyOption.BusySpin);

            _stepOneFunctionEventHandler = new LatencyStepEventHandler(FunctionStep.One, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepTwoFunctionEventHandler = new LatencyStepEventHandler(FunctionStep.Two, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);
            _stepThreeFunctionEventHandler = new LatencyStepEventHandler(FunctionStep.Three, Histogram, StopwatchTimestampCostInNano, TicksToNanos, Iterations);

            _ringBuffer.ProcessWith(_stepOneFunctionEventHandler)
                .Then(_stepTwoFunctionEventHandler)
                .Then(_stepThreeFunctionEventHandler);
        }

        public override void RunPass()
        {
            _ringBuffer.StartProcessors();

            for (long i = 0; i < Iterations; i++)
            {
                ValueEvent data;
                var sequence = _ringBuffer.NextEvent(out data);
                data.Value = Stopwatch.GetTimestamp();
                _ringBuffer.Publish(sequence);

                var pauseStart = Stopwatch.GetTimestamp();
                while (PauseNanos > (Stopwatch.GetTimestamp() - pauseStart) * TicksToNanos)
                {
                    // busy spin
                }
            }

            while (!_stepThreeFunctionEventHandler.Done)
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