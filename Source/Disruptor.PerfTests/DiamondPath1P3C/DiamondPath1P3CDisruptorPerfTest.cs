using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.DiamondPath1P3C
{
    [TestFixture]
    public class DiamondPath1P3CDisruptorPerfTest:AbstractDiamondPath1P3CPerfTest
    {
        private readonly RingBuffer<FizzBuzzEvent> _ringBuffer;
        private readonly FizzBuzzEventHandler _fizzEventHandler;
        private readonly FizzBuzzEventHandler _buzzEventHandler;
        private readonly FizzBuzzEventHandler _fizzBuzzEventHandler;

        public DiamondPath1P3CDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<FizzBuzzEvent>(() => new FizzBuzzEvent(), Size,
                                          ClaimStrategyOption.SingleProducer,
                                          WaitStrategyOption.Yielding);


            _fizzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.Fizz, Iterations);
            _buzzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.Buzz, Iterations);
            _fizzBuzzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.FizzBuzz, Iterations);

            _ringBuffer.ProcessWith(_fizzEventHandler, _buzzEventHandler)
                       .Then(_fizzBuzzEventHandler);
        }

        public override long RunPass()
        {
            _ringBuffer.StartProcessors();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                FizzBuzzEvent data;
                var sequence = _ringBuffer.NextEvent(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }

            while (!_fizzBuzzEventHandler.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _ringBuffer.Halt();

            Assert.AreEqual(ExpectedResult, _fizzBuzzEventHandler.FizzBuzzCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}