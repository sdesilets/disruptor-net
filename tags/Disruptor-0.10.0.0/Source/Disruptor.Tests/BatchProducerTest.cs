using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class BatchProducerTest
    {
        private RingBuffer<StubData> _ringBuffer;
        private IConsumerBarrier<StubData> _consumerBarrier;
        private IReferenceTypeProducerBarrier<StubData> _producerBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubData>(() => new StubData(-1), 20);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _producerBarrier = _ringBuffer.CreateProducerBarrier(new NoOpConsumer<StubData>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimBatchAndCommitBack() 
        {
            const int batchSize = 5;

            var sequenceBatch = _producerBarrier.NextEntries(batchSize);

            Assert.AreEqual(0L, sequenceBatch.Start);
            Assert.AreEqual(4L, sequenceBatch.End);
            Assert.AreEqual(RingBufferConvention.InitialCursorValue, _ringBuffer.Cursor);

            _producerBarrier.Commit(sequenceBatch);

            Assert.AreEqual(batchSize - 1, _ringBuffer.Cursor);
            Assert.AreEqual(batchSize - 1, _consumerBarrier.WaitFor(0L));
        }
    }
}