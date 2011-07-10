using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.UniCast1P1CBatch
{
    [TestFixture]
    public class UniCast1P1CBatchDisruptorPerfTest:AbstractUniCast1P1CBatchPerfTest
    {
        private readonly RingBuffer<ValueEntry> _ringBuffer;
        private readonly IConsumerBarrier<ValueEntry> _consumerBarrier;
        private readonly ValueAdditionHandler2 _handler;
        private readonly BatchConsumer<ValueEntry> _batchConsumer;
        private readonly IReferenceTypeProducerBarrier<ValueEntry> _producerBarrier;

        public UniCast1P1CBatchDisruptorPerfTest()
        {
            _ringBuffer = new RingBuffer<ValueEntry>(()=>new ValueEntry(), Size,
                                   ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                   WaitStrategyFactory.WaitStrategyOption.Yielding);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _handler = new ValueAdditionHandler2();
            _batchConsumer = new BatchConsumer<ValueEntry>(_consumerBarrier, _handler);
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumer);
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }

        public override long RunPass()
        {
            (new Thread(_batchConsumer.Run){Name = "Batch consumer"}).Start();

            const int batchSize = 10;

            var sw = Stopwatch.StartNew();

            long offset = 0;
            for (long i = 0; i < Iterations; i += batchSize)
            {
                var sequenceBatch = _producerBarrier.NextEntries(batchSize);
                for (long sequence = sequenceBatch.Start; sequence <= sequenceBatch.End; sequence++)
                {
                    ValueEntry entry = _producerBarrier.GetEntry(sequence);
                    entry.Value = offset++;
                }
                _producerBarrier.Commit(sequenceBatch);
            }

            long expectedSequence = _ringBuffer.Cursor;
            while (_batchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            long opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _batchConsumer.Halt();

            Assert.AreEqual(ExpectedResult, _handler.Value.Value);

            return opsPerSecond;
        }
    }
}