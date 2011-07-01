using System;
using System.Threading;
using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class BatchConsumerSequenceTrackingCallbackTests
    {
        private readonly CountdownEvent _onAvailableCountdown = new CountdownEvent(2);
        private readonly CountdownEvent _readyToCallbackCountdown = new CountdownEvent(1);

        [Test]
        [Ignore("Test to fix, the Sequence in BatchConsumer appears to not increment properly, have to dig...")]
        public void ShouldReportProgressByUpdatingSequenceViaCallback()
        {
            var ringBuffer = new RingBuffer<StubData>(() => new StubData(-1), 16);
            var consumerBarrier = ringBuffer.CreateConsumerBarrier();
            var handler = new TestSequenceTrackingHandler(_onAvailableCountdown, _readyToCallbackCountdown);
            var batchConsumer = new BatchConsumer<StubData>(consumerBarrier, handler);
            var producerBarrier = ringBuffer.CreateProducerBarrier(batchConsumer);
            StubData data;

            var thread = new Thread(batchConsumer.Run);
            thread.Start();

            Assert.AreEqual(-1L, batchConsumer.Sequence);
            producerBarrier.Commit(producerBarrier.NextEntry(out data));
            producerBarrier.Commit(producerBarrier.NextEntry(out data));
            _onAvailableCountdown.Wait();
            Assert.AreEqual(-1L, batchConsumer.Sequence);

            producerBarrier.Commit(producerBarrier.NextEntry(out data));
            _readyToCallbackCountdown.Wait();
            Assert.AreEqual(2L, batchConsumer.Sequence);

            batchConsumer.Halt();
            thread.Join();
        }

        private class TestSequenceTrackingHandler : ISequenceTrackingHandler<StubData>
        {
            private readonly CountdownEvent _onAvailableCountdown;
            private readonly CountdownEvent _readyToCallbackCountdown;
            private BatchConsumer<StubData>.SequenceTrackerCallback _sequenceTrackerCallback;

            public TestSequenceTrackingHandler(CountdownEvent onAvailableCountdown, CountdownEvent readyToCallbackCountdown)
            {
                _onAvailableCountdown = onAvailableCountdown;
                _readyToCallbackCountdown = readyToCallbackCountdown;
            }

            public void OnAvailable(long sequence, StubData data)
            {
                Console.WriteLine("Recieved: {0} with sequence {1}", data, sequence);
                if (sequence == 2L)
                {
                    _sequenceTrackerCallback.OnComplete(sequence);
                    _readyToCallbackCountdown.Signal();
                }
                else
                {
                    _onAvailableCountdown.Signal();
                }
            }

            public void OnEndOfBatch()
            {
            }

            public void SetSequenceTrackerCallback(BatchConsumer<StubData>.SequenceTrackerCallback sequenceTrackerCallback)
            {
                _sequenceTrackerCallback = sequenceTrackerCallback;
            }
        }
    }
}