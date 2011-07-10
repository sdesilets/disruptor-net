using System.Threading;
using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class BatchConsumerSequenceTrackingCallbackTests
    {
        private readonly CountdownEvent _callbackCountdown = new CountdownEvent(1);
        private readonly CountdownEvent _endOfBatchCountdown = new CountdownEvent(1);

        [Test]
        public void ShouldReportProgressByUpdatingSequenceViaCallback()
        {
            var ringBuffer = new RingBuffer<StubData>(() => new StubData(-1), 16);
            var consumerBarrier = ringBuffer.CreateConsumerBarrier();
            var handler = new TestSequenceTrackingHandler(_callbackCountdown, _endOfBatchCountdown);
            var batchConsumer = new BatchConsumer<StubData>(consumerBarrier, handler);
            var producerBarrier = ringBuffer.CreateProducerBarrier(batchConsumer);
            StubData data;

            var thread = new Thread(batchConsumer.Run) {IsBackground = true};
            thread.Start();

            Assert.AreEqual(-1L, batchConsumer.Sequence);
            producerBarrier.Commit(producerBarrier.NextEntry(out data));

            _callbackCountdown.Wait();
            Assert.AreEqual(0L, batchConsumer.Sequence);

            _endOfBatchCountdown.Signal();
            Assert.AreEqual(0L, batchConsumer.Sequence);

            batchConsumer.Halt();
            thread.Join();
        }

        private class TestSequenceTrackingHandler : ISequenceTrackingHandler<StubData>
        {
            private readonly CountdownEvent _callbackCountdown;
            private readonly CountdownEvent _endOfBatchCountdown;
            private BatchConsumer<StubData>.SequenceTrackerCallback _sequenceTrackerCallback;

            public TestSequenceTrackingHandler(CountdownEvent callbackCountdown, CountdownEvent endOfBatchCountdown)
            {
                _callbackCountdown = callbackCountdown;
                _endOfBatchCountdown = endOfBatchCountdown;
            }

            public void OnAvailable(long sequence, StubData data)
            {
                _sequenceTrackerCallback.OnComplete(sequence);
                _callbackCountdown.Signal();
            }

            public void OnEndOfBatch()
            {
                _endOfBatchCountdown.Wait();
            }

            public void SetSequenceTrackerCallback(BatchConsumer<StubData>.SequenceTrackerCallback sequenceTrackerCallback)
            {
                _sequenceTrackerCallback = sequenceTrackerCallback;
            }
        }
    }
}