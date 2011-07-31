using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.DiamondPath1P3C
{
    [TestFixture]
    public class DiamondPath1P3CDisruptorPerfTest:AbstractDiamondPath1P3CPerfTest
    {
        private readonly RingBuffer<FizzBuzzEntry> _ringBuffer;
        private readonly FizzBuzzHandler _fizzHandler;
        private readonly FizzBuzzHandler _buzzHandler;
        private readonly FizzBuzzHandler _fizzBuzzHandler;

        public DiamondPath1P3CDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<FizzBuzzEntry>(() => new FizzBuzzEntry(), Size,
                                          ClaimStrategyOption.SingleProducer,
                                          WaitStrategyOption.Yielding);


            _fizzHandler = new FizzBuzzHandler(FizzBuzzStep.Fizz, Iterations);
            _buzzHandler = new FizzBuzzHandler(FizzBuzzStep.Buzz, Iterations);
            _fizzBuzzHandler = new FizzBuzzHandler(FizzBuzzStep.FizzBuzz, Iterations);

            _ringBuffer.ConsumeWith(_fizzHandler, _buzzHandler)
                       .Then(_fizzBuzzHandler);
        }

        public override long RunPass()
        {
            _ringBuffer.StartConsumers();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                FizzBuzzEntry data;
                var sequence = _ringBuffer.NextEntry(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }

            while (!_fizzBuzzHandler.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _ringBuffer.Halt();

            Assert.AreEqual(ExpectedResult, _fizzBuzzHandler.FizzBuzzCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}