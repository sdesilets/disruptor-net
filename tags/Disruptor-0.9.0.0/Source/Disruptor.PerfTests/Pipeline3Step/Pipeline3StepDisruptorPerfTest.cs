using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3Step
{
    [TestFixture]
    public class Pipeline3StepDisruptorPerfTest : AbstractPipeline3StepPerfTest
    {
        private readonly RingBuffer<FunctionEntry> _ringBuffer;

        private readonly IConsumerBarrier<FunctionEntry> _stepOneConsumerBarrier;
        private readonly BatchConsumer<FunctionEntry> _stepOneBatchConsumer;

        private readonly IConsumerBarrier<FunctionEntry> _stepTwoConsumerBarrier;
        private readonly BatchConsumer<FunctionEntry> _stepTwoBatchConsumer;

        private readonly IConsumerBarrier<FunctionEntry> _stepThreeConsumerBarrier;
        private readonly FunctionHandler _stepThreeFunctionHandler;
        private readonly BatchConsumer<FunctionEntry> _stepThreeBatchConsumer;

        private readonly IReferenceTypeProducerBarrier<FunctionEntry> _producerBarrier;

        public Pipeline3StepDisruptorPerfTest()
        {
            _ringBuffer = new RingBuffer<FunctionEntry>(() => new FunctionEntry(), Size,
                                      ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                      WaitStrategyFactory.WaitStrategyOption.Yielding);

            _stepOneConsumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _stepOneBatchConsumer = new BatchConsumer<FunctionEntry>(_stepOneConsumerBarrier, new FunctionHandler(FunctionStep.One));

            _stepTwoConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepOneBatchConsumer);
            _stepTwoBatchConsumer = new BatchConsumer<FunctionEntry>(_stepTwoConsumerBarrier, new FunctionHandler(FunctionStep.Two));

            _stepThreeConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepTwoBatchConsumer);
            _stepThreeFunctionHandler = new FunctionHandler(FunctionStep.Three);
            _stepThreeBatchConsumer = new BatchConsumer<FunctionEntry>(_stepThreeConsumerBarrier, _stepThreeFunctionHandler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier(_stepThreeBatchConsumer);
        }

        public override long RunPass()
        {
            _stepThreeFunctionHandler.Reset();

            new Thread(_stepOneBatchConsumer.Run) { Name = "Step 1" }.Start();
            new Thread(_stepTwoBatchConsumer.Run) { Name = "Step 2" }.Start();
            new Thread(_stepThreeBatchConsumer.Run) { Name = "Step 3" }.Start();

            var sw = Stopwatch.StartNew();

            var operandTwo = OperandTwoInitialValue;
            for (long i = 0; i < Iterations; i++)
            {
                FunctionEntry data;
                var sequence = _producerBarrier.NextEntry(out data);
                data.OperandOne = i;
                data.OperandTwo = operandTwo--;
                _producerBarrier.Commit(sequence);
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_stepThreeBatchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _stepOneBatchConsumer.Halt();
            _stepTwoBatchConsumer.Halt();
            _stepThreeBatchConsumer.Halt();

            Assert.AreEqual(ExpectedResult, _stepThreeFunctionHandler.StepThreeCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}