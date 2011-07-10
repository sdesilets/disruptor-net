using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3StepLatency
{
    [TestFixture]
    public sealed class Pipeline3StepLatencyDisruptorPerfTest : AbstractPipeline3StepLatencyPerfTest
    {
        private readonly ValueTypeRingBuffer<long> _ringBuffer;

        private readonly IConsumerBarrier<long> _stepOneConsumerBarrier;
        private readonly LatencyStepHandler _stepOneFunctionHandler;
        private readonly BatchConsumer<long> _stepOneBatchConsumer;

        private readonly IConsumerBarrier<long> _stepTwoConsumerBarrier;
        private readonly LatencyStepHandler _stepTwoFunctionHandler;
        private readonly BatchConsumer<long> _stepTwoBatchConsumer;

        private readonly IConsumerBarrier<long> _stepThreeConsumerBarrier;
        private readonly LatencyStepHandler _stepThreeFunctionHandler;
        private readonly BatchConsumer<long> _stepThreeBatchConsumer;

        private readonly IValueTypeProducerBarrier<long> _producerBarrier;

        public Pipeline3StepLatencyDisruptorPerfTest()
        {
            _ringBuffer = new ValueTypeRingBuffer<long>(Size,
                                   ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                   WaitStrategyFactory.WaitStrategyOption.BusySpin);
            _stepOneConsumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _stepOneFunctionHandler = new LatencyStepHandler(FunctionStep.One, Histogram, StopwatchTimestampCostInNano, TicksToNanos);
            _stepOneBatchConsumer = new BatchConsumer<long>(_stepOneConsumerBarrier, _stepOneFunctionHandler);

            _stepTwoConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepOneBatchConsumer);
            _stepTwoFunctionHandler = new LatencyStepHandler(FunctionStep.Two, Histogram, StopwatchTimestampCostInNano, TicksToNanos);
            _stepTwoBatchConsumer = new BatchConsumer<long>(_stepTwoConsumerBarrier, _stepTwoFunctionHandler);

            _stepThreeConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepTwoBatchConsumer);
            _stepThreeFunctionHandler = new LatencyStepHandler(FunctionStep.Three, Histogram, StopwatchTimestampCostInNano, TicksToNanos);
            _stepThreeBatchConsumer = new BatchConsumer<long>(_stepThreeConsumerBarrier, _stepThreeFunctionHandler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier(_stepThreeBatchConsumer);
        }

        public override void RunPass()
        {
            new Thread(_stepOneBatchConsumer.Run) { Name = "Step 1 disruptor" }.Start();
            new Thread(_stepTwoBatchConsumer.Run) { Name = "Step 2 disruptor" }.Start();
            new Thread(_stepThreeBatchConsumer.Run) { Name = "Step 3 disruptor" }.Start();

            for (long i = 0; i < Iterations; i++)
            {
                _producerBarrier.Commit(Stopwatch.GetTimestamp());

                var pauseStart = Stopwatch.GetTimestamp();
                while (PauseNanos > (Stopwatch.GetTimestamp() - pauseStart) * TicksToNanos)
                {
                    // busy spin
                }
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_stepThreeBatchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            _stepOneBatchConsumer.Halt();
            _stepTwoBatchConsumer.Halt();
            _stepThreeBatchConsumer.Halt();
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}