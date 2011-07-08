using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.DiamondPath1P3C
{
    [TestFixture]
    public class DiamondPath1P3CBlockingCollectionPerfTest:AbstractDiamondPath1P3CPerfTest
    {
        private readonly BlockingCollection<long> _fizzInputQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _buzzInputQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<bool> _fizzOutputQueue = new BlockingCollection<bool>(Size);
        private readonly BlockingCollection<bool> _buzzOutputQueue = new BlockingCollection<bool>(Size);

        private readonly FizzBuzzQueueConsumer _fizzQueueConsumer;
        private readonly FizzBuzzQueueConsumer _buzzQueueConsumer;
        private readonly FizzBuzzQueueConsumer _fizzBuzzQueueConsumer;
        
        public DiamondPath1P3CBlockingCollectionPerfTest()
        {
            _fizzQueueConsumer = new FizzBuzzQueueConsumer(FizzBuzzStep.Fizz, _fizzInputQueue, _buzzInputQueue, _fizzOutputQueue, _buzzOutputQueue, Iterations);
            _buzzQueueConsumer = new FizzBuzzQueueConsumer(FizzBuzzStep.Buzz, _fizzInputQueue, _buzzInputQueue, _fizzOutputQueue, _buzzOutputQueue, Iterations);
            _fizzBuzzQueueConsumer = new FizzBuzzQueueConsumer(FizzBuzzStep.FizzBuzz, _fizzInputQueue, _buzzInputQueue, _fizzOutputQueue, _buzzOutputQueue, Iterations);
        }

        public override long RunPass()
        {
            _fizzBuzzQueueConsumer.Reset();

            (new Thread(_fizzQueueConsumer.Run) { Name = "Fizz" }).Start();
            (new Thread(_buzzQueueConsumer.Run) { Name = "Buzz" }).Start();
            (new Thread(_fizzBuzzQueueConsumer.Run) { Name = "FizzBuzz" }).Start();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                _fizzInputQueue.Add(i);
                _buzzInputQueue.Add(i);
            }

            while (!_fizzBuzzQueueConsumer.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            Assert.AreEqual(ExpectedResult, _fizzBuzzQueueConsumer.FizzBuzzCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}