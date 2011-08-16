using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class BatchProducerTest
    {
        private RingBuffer<StubData> _ringBuffer;
        private IDependencyBarrier<StubData> _dependencyBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubData>(() => new StubData(-1), 20);
            _dependencyBarrier = _ringBuffer.CreateBarrier();
            _ringBuffer.SetTrackedEventProcessors(new NoOpEventProcessor<StubData>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimBatchAndCommitBack() 
        {
            const int batchSize = 5;

            var sequenceBatch = _ringBuffer.NextEvents(batchSize);

            Assert.AreEqual(0L, sequenceBatch.Start);
            Assert.AreEqual(4L, sequenceBatch.End);
            Assert.AreEqual(RingBufferConvention.InitialCursorValue, _ringBuffer.Cursor);

            _ringBuffer.Commit(sequenceBatch);

            Assert.AreEqual(batchSize - 1, _ringBuffer.Cursor);
            Assert.AreEqual(batchSize - 1, _dependencyBarrier.WaitFor(0L).AvailableSequence);
        }
    }
}