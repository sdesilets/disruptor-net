using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.DiamondPath1P3C
{
    [TestFixture]
    public class DiamondPath1P3CDisruptorPerfTest:AbstractDiamondPath1P3CPerfTest
    {
        private readonly RingBuffer<FizzBuzzEntry> _ringBuffer;
        private readonly IConsumerBarrier<FizzBuzzEntry> _consumerBarrier;

        private readonly FizzBuzzHandler _fizzHandler;
        private readonly BatchConsumer<FizzBuzzEntry> _batchConsumerFizz;

        private readonly FizzBuzzHandler _buzzHandler;
        private readonly BatchConsumer<FizzBuzzEntry> _batchConsumerBuzz;

        private readonly IConsumerBarrier<FizzBuzzEntry> _consumerBarrierFizzBuzz;

        private readonly FizzBuzzHandler _fizzBuzzHandler;
        private readonly BatchConsumer<FizzBuzzEntry> _batchConsumerFizzBuzz;

        private readonly IReferenceTypeProducerBarrier<FizzBuzzEntry> _producerBarrier;

        public DiamondPath1P3CDisruptorPerfTest()
        {
            _ringBuffer = new RingBuffer<FizzBuzzEntry>(() => new FizzBuzzEntry(), Size,
                                          ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                          WaitStrategyFactory.WaitStrategyOption.Yielding);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _fizzHandler = new FizzBuzzHandler(FizzBuzzStep.Fizz);
            _batchConsumerFizz = new BatchConsumer<FizzBuzzEntry>(_consumerBarrier, _fizzHandler);

            _buzzHandler = new FizzBuzzHandler(FizzBuzzStep.Buzz);
            _batchConsumerBuzz = new BatchConsumer<FizzBuzzEntry>(_consumerBarrier, _buzzHandler);
            _consumerBarrierFizzBuzz = _ringBuffer.CreateConsumerBarrier(_batchConsumerFizz, _batchConsumerBuzz);

            _fizzBuzzHandler = new FizzBuzzHandler(FizzBuzzStep.FizzBuzz);
            _batchConsumerFizzBuzz = new BatchConsumer<FizzBuzzEntry>(_consumerBarrierFizzBuzz, _fizzBuzzHandler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumerFizzBuzz);
        }

        public override long RunPass()
        {
            _fizzBuzzHandler.Reset();

            (new Thread(_batchConsumerFizz.Run) { Name = "Fizz" }).Start();
            (new Thread(_batchConsumerBuzz.Run) { Name = "Buzz" }).Start();
            (new Thread(_batchConsumerFizzBuzz.Run) { Name = "FizzBuzz" }).Start();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                FizzBuzzEntry data;
                var sequence = _producerBarrier.NextEntry(out data);
                data.Value = i;
                _producerBarrier.Commit(sequence);
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_batchConsumerFizzBuzz.Sequence < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _batchConsumerFizz.Halt();
            _batchConsumerBuzz.Halt();
            _batchConsumerFizzBuzz.Halt();

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