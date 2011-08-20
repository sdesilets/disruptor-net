using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class BatchPublisherTest
    {
        private RingBuffer<StubEvent> _ringBuffer;
        private IDependencyBarrier _dependencyBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubEvent>(() => new StubEvent(-1), 20);
            _dependencyBarrier = _ringBuffer.CreateDependencyBarrier();
            _ringBuffer.SetTrackedEventProcessors(new NoOpEventProcessor<StubEvent>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimBatchAndCommitBack() 
        {
            const int batchSize = 5;

            var sequenceBatch = _ringBuffer.NextEvents(batchSize);

            Assert.AreEqual(0L, sequenceBatch.Start);
            Assert.AreEqual(4L, sequenceBatch.End);
            Assert.AreEqual(RingBufferConvention.InitialCursorValue, _ringBuffer.Cursor);

            _ringBuffer.Publish(sequenceBatch);

            Assert.AreEqual(batchSize - 1, _ringBuffer.Cursor);
            Assert.AreEqual(batchSize - 1, _dependencyBarrier.WaitFor(0L).AvailableSequence);
        }
    }
}