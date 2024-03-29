using System.Diagnostics;
using System.Threading;
using Disruptor.Dsl;
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
        private readonly ManualResetEvent _mru;
        private readonly Disruptor<FizzBuzzEvent> _disruptor;

        public DiamondPath1P3CDisruptorPerfTest()
            : base(100 * Million)
        {
            _disruptor = new Disruptor<FizzBuzzEvent>(() => new FizzBuzzEvent(),
                                                      new SingleThreadedClaimStrategy(Size),
                                                      new YieldingWaitStrategy());

            _mru = new ManualResetEvent(false);
            _fizzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.Fizz, Iterations, _mru);
            _buzzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.Buzz, Iterations, _mru);
            _fizzBuzzEventHandler = new FizzBuzzEventHandler(FizzBuzzStep.FizzBuzz, Iterations, _mru);

            _disruptor.HandleEventsWith(_fizzEventHandler, _buzzEventHandler)
                      .Then(_fizzBuzzEventHandler);
            _ringBuffer = _disruptor.RingBuffer;
        }

        public override long RunPass()
        {
            _disruptor.Start();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                var sequence = _ringBuffer.Next();
                _ringBuffer[sequence].Value = i;
                _ringBuffer.Publish(sequence);
            }

            _mru.WaitOne();

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _disruptor.Shutdown();

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