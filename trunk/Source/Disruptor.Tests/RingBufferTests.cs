using System;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Tests.Support;
using NUnit.Framework;
using Gen = System.Collections.Generic;

namespace Disruptor.Tests
{
    [TestFixture]
    public class RingBufferTests
    {
        private RingBuffer<StubEntry> _ringBuffer;
        private IConsumerBarrier<StubEntry> _consumerBarrier;
        private IProducerBarrier<StubEntry> _producerBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubEntry>(new StubEntryFactory(), 20);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _producerBarrier = _ringBuffer.CreateProducerBarrier(new NoOpConsumer<StubEntry>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimAndGet() 
        {
            Assert.AreEqual(RingBuffer<StubEntry>.InitialCursorValue, _ringBuffer.Cursor);

            var expectedEntry = new StubEntry(2701);

            var oldEntry = _producerBarrier.NextEntry();
            oldEntry.Copy(expectedEntry);
            _producerBarrier.Commit(oldEntry);

            var sequence = _consumerBarrier.WaitFor(0);
            Assert.AreEqual(0L, sequence);

            var entry = _ringBuffer.GetEntry(sequence);
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(0L, _ringBuffer.Cursor);
        }

        [Test]
        public void ShouldClaimAndGetWithTimeout() 
        {
            Assert.AreEqual(RingBuffer<StubEntry>.InitialCursorValue, _ringBuffer.Cursor);

            var expectedEntry = new StubEntry(2701);

            var oldEntry = _producerBarrier.NextEntry();
            oldEntry.Copy(expectedEntry);
            _producerBarrier.Commit(oldEntry);

            var sequence = _consumerBarrier.WaitFor(0, TimeSpan.FromMilliseconds(5));
            Assert.AreEqual(0, sequence);

            var entry = _ringBuffer.GetEntry(sequence);
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(0L, _ringBuffer.Cursor);
        }

        [Test]
        public void ShouldGetWithTimeout()
        {
            var sequence = _consumerBarrier.WaitFor(0, TimeSpan.FromMilliseconds(5));
            Assert.AreEqual(RingBuffer<StubEntry>.InitialCursorValue, sequence);
        }

        [Test]
        public void ShouldClaimAndGetInSeparateThread()
        {
            var messages = GetMessages(0, 0);

            var expectedEntry = new StubEntry(2701);

            var oldEntry = _producerBarrier.NextEntry();
            oldEntry.Copy(expectedEntry);
            _producerBarrier.Commit(oldEntry);

            Assert.AreEqual(expectedEntry, messages.Result[0]);
        }

        [Test]
        public void ShouldClaimAndGetMultipleMessages() 
        {
            var numMessages = _ringBuffer.Capacity;
            for (var i = 0; i < numMessages; i++)
            {
                var entry = _producerBarrier.NextEntry();
                entry.Value = i;
                _producerBarrier.Commit(entry);
            }

            var expectedSequence = numMessages - 1;
            var available = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (var i = 0; i < numMessages; i++)
            {
                Assert.AreEqual(i, _ringBuffer.GetEntry(i).Value);
            }
        }

        [Test]
        public void ShouldWrap()
        {
            var numMessages = _ringBuffer.Capacity;
            const int offset = 1000;
            for (var i = 0; i < numMessages + offset ; i++)
            {
                var entry = _producerBarrier.NextEntry();
                entry.Value = i;
                _producerBarrier.Commit(entry);
            }

            var expectedSequence = numMessages + offset - 1;
            var available = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (var i = offset; i < numMessages + offset; i++)
            {
                Assert.AreEqual(i, _ringBuffer.GetEntry(i).Value);
            }
        }

        [Test]
        public void ShouldSetAtSpecificSequence()
        {
            const long expectedSequence = 5;
            var forceFillProducerBarrier = _ringBuffer.CreateForceFillProducerBarrier(new NoOpConsumer<StubEntry>(_ringBuffer));

            var expectedEntry = forceFillProducerBarrier.ClaimEntry(expectedSequence);
            expectedEntry.Value = (int)expectedSequence;
            forceFillProducerBarrier.Commit(expectedEntry);

            var sequence = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, sequence);

            StubEntry entry = _ringBuffer.GetEntry(sequence);
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(expectedSequence, _ringBuffer.Cursor);
        }

        private Task<Gen.List<StubEntry>> GetMessages(long initial, long toWaitFor)
        {
            var barrier = new Barrier(2);
            var consumerBarrier = _ringBuffer.CreateConsumerBarrier();

            var testWaiter = new TestWaiter(barrier, consumerBarrier, initial, toWaitFor);
            var task = Task.Factory.StartNew(() => testWaiter.Call());

            barrier.SignalAndWait();

            return task;
        }
    }
}
