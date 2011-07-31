using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3Step
{
    [TestFixture]
    public class Pipeline3StepDisruptorPerfTest : AbstractPipeline3StepPerfTest
    {
        private readonly RingBuffer<FunctionEntry> _ringBuffer;
        private readonly FunctionHandler _stepThreeFunctionHandler;
        private readonly IProducerBarrier<FunctionEntry> _producerBarrier;

        public Pipeline3StepDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<FunctionEntry>(() => new FunctionEntry(), Size,
                                                        ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                                        WaitStrategyFactory.WaitStrategyOption.Yielding);

            _stepThreeFunctionHandler = new FunctionHandler(FunctionStep.Three, Iterations);

            _ringBuffer.ConsumeWith(new FunctionHandler(FunctionStep.One, Iterations))
                .Then(new FunctionHandler(FunctionStep.Two, Iterations))
                .Then(_stepThreeFunctionHandler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier();
        }

        public override long RunPass()
        {
            _ringBuffer.StartConsumers();

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

            while (!_stepThreeFunctionHandler.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _ringBuffer.Halt();

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