using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3Step
{
    [TestFixture]
    public class Pipeline3StepDisruptorPerfTest : AbstractPipeline3StepPerfTest
    {
        private readonly RingBuffer<FunctionEvent> _ringBuffer;
        private readonly FunctionEventHandler _stepThreeFunctionEventHandler;

        public Pipeline3StepDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<FunctionEvent>(() => new FunctionEvent(), Size,
                                                        ClaimStrategyOption.SingleProducer,
                                                        WaitStrategyOption.Yielding);

            _stepThreeFunctionEventHandler = new FunctionEventHandler(FunctionStep.Three, Iterations);

            _ringBuffer.HandleEventsWith(new FunctionEventHandler(FunctionStep.One, Iterations))
                .Then(new FunctionEventHandler(FunctionStep.Two, Iterations))
                .Then(_stepThreeFunctionEventHandler);
        }

        public override long RunPass()
        {
            _ringBuffer.StartProcessors();

            var sw = Stopwatch.StartNew();

            var operandTwo = OperandTwoInitialValue;
            for (long i = 0; i < Iterations; i++)
            {
                var evt = _ringBuffer.NextEvent();
                evt.Data.OperandOne = i;
                evt.Data.OperandTwo = operandTwo--;
                _ringBuffer.Publish(evt);
            }

            while (!_stepThreeFunctionEventHandler.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _ringBuffer.Halt();

            Assert.AreEqual(ExpectedResult, _stepThreeFunctionEventHandler.StepThreeCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}