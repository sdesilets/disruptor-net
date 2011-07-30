using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class ValueTypeBatchProducerTest
    {
        private ValueTypeRingBuffer<long> _ringBuffer;
        private IConsumerBarrier<long> _consumerBarrier;
        private IValueTypeProducerBarrier<long> _producerBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new ValueTypeRingBuffer<long>(20);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _producerBarrier = _ringBuffer.CreateProducerBarrier(new NoOpConsumer<StubData>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimBatchAndCommitBack()
        {
            const int batchSize = 5;

            var batch = new[] {1L, 2L, 3L, 4L, 5L};

            _producerBarrier.Commit(batch);

            Assert.AreEqual(batchSize - 1, _ringBuffer.Cursor);
            Assert.AreEqual(batchSize - 1, _consumerBarrier.WaitFor(0L));

            for (int i = 0; i < batchSize; i++)
            {
                Assert.AreEqual(i + 1L, _ringBuffer[i].Data);
            }
        }
    }
}