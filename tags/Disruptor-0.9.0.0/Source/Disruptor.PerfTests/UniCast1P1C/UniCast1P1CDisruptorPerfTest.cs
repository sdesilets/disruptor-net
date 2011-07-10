using System.Diagnostics;
using System.Threading.Tasks;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.UniCast1P1C
{
    [TestFixture]
    public class UniCast1P1CDisruptorPerfTest : AbstractUniCast1P1CPerfTest
    {
        private readonly ValueTypeRingBuffer<long> _ringBuffer;
        private readonly IConsumerBarrier<long> _consumerBarrier;
        private readonly ValueAdditionHandler _handler;
        private readonly BatchConsumer<long> _batchConsumer;
        private readonly IValueTypeProducerBarrier<long> _producerBarrier;

        public UniCast1P1CDisruptorPerfTest()
        {

            _ringBuffer = new ValueTypeRingBuffer<long>(Size,
                                                     ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                                     WaitStrategyFactory.WaitStrategyOption.Yielding);

            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _handler = new ValueAdditionHandler();
            _batchConsumer = new BatchConsumer<long>(_consumerBarrier, _handler);
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumer);
        }

        public override long RunPass()
        {
            _handler.Reset();

            Task.Factory.StartNew(_batchConsumer.Run);

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                _producerBarrier.Commit(i);
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_batchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / (sw.ElapsedMilliseconds);
            _batchConsumer.Halt();

            Assert.AreEqual(ExpectedResult, _handler.Value, "RunDisruptorPass");

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}