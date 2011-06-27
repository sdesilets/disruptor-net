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
        [Ignore("Test to fix, the Sequence in BatchConsumer appears to to increment properly, have to dig...")]
        public void ShouldReportProgressByUpdatingSequenceViaCallback()
        {
            var ringBuffer = new RingBuffer<StubEntry>(new StubEntryFactory(), 16);
            var consumerBarrier = ringBuffer.CreateConsumerBarrier();
            var handler = new TestSequenceTrackingHandler(_onAvailableCountdown, _readyToCallbackCountdown);
            var batchConsumer = new BatchConsumer<StubEntry>(consumerBarrier, handler);
            var producerBarrier = ringBuffer.CreateProducerBarrier(batchConsumer);

            var thread = new Thread(batchConsumer.Run);
            thread.Start();

            Assert.AreEqual(-1L, batchConsumer.Sequence);
            var stubEntry = producerBarrier.NextEntry();
            stubEntry.TestString = "First Message";
            stubEntry.Value = 0;
            Console.WriteLine("Publishing: " + stubEntry);
            producerBarrier.Commit(stubEntry);

            stubEntry = producerBarrier.NextEntry();
            stubEntry.TestString = "Second Message";
            stubEntry.Value = 1;
            Console.WriteLine("Publishing: " + stubEntry);
            producerBarrier.Commit(stubEntry);
            _onAvailableCountdown.Wait();
            Assert.AreEqual(-1L, batchConsumer.Sequence);

            stubEntry = producerBarrier.NextEntry();
            Console.WriteLine("Publishing: " + stubEntry);
            stubEntry.TestString = "Last Message";
            stubEntry.Value = 2;
            producerBarrier.Commit(stubEntry);
            _readyToCallbackCountdown.Wait();
            Assert.AreEqual(2L, batchConsumer.Sequence);

            batchConsumer.Halt();
            thread.Join();
        }

        private class TestSequenceTrackingHandler : ISequenceTrackingHandler<StubEntry>
        {
            private readonly CountdownEvent _onAvailableCountdown;
            private readonly CountdownEvent _readyToCallbackCountdown;
            private BatchConsumer<StubEntry>.SequenceTrackerCallback _sequenceTrackerCallback;

            public TestSequenceTrackingHandler(CountdownEvent onAvailableCountdown, CountdownEvent readyToCallbackCountdown)
            {
                _onAvailableCountdown = onAvailableCountdown;
                _readyToCallbackCountdown = readyToCallbackCountdown;
            }

            public void OnAvailable(StubEntry entry)
            {
                Console.WriteLine("Recieved: " + entry);
                if (entry.Sequence == 2L)
                {
                    _sequenceTrackerCallback.OnComplete(entry.Sequence);
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

            public void OnCompletion()
            {
            }

            public void SetSequenceTrackerCallback(BatchConsumer<StubEntry>.SequenceTrackerCallback sequenceTrackerCallback)
            {
                _sequenceTrackerCallback = sequenceTrackerCallback;
            }
        }
    }
}