using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.UniCast1P1CBatch
{
    [TestFixture]
    public class UniCast1P1CBatchDisruptorPerfTest:AbstractUniCast1P1CBatchPerfTest
    {
        private readonly RingBuffer<ValueEntry> _ringBuffer;
        private readonly ValueAdditionHandler _handler;
        private readonly IProducerBarrier<ValueEntry> _producerBarrier;

        public UniCast1P1CBatchDisruptorPerfTest()
            : base(100 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEntry>(()=>new ValueEntry(), Size,
                                   ClaimStrategyOption.SingleProducer,
                                   WaitStrategyOption.Yielding);

            _handler = new ValueAdditionHandler(Iterations);
            _ringBuffer.ConsumeWith(_handler);
            _producerBarrier = _ringBuffer.CreateProducerBarrier();
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }

        public override long RunPass()
        {
            _ringBuffer.StartConsumers();

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

            while (!_handler.Done)
            {
                // busy spin
            }

            long opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _ringBuffer.Halt();

            Assert.AreEqual(ExpectedResult, _handler.Value.Value);

            return opsPerSecond;
        }
    }
}