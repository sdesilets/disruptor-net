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
        private RingBuffer<StubData> _ringBuffer;
        private IConsumerBarrier<StubData> _consumerBarrier;
        private IReferenceTypeProducerBarrier<StubData> _producerBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubData>(()=>new StubData(-1), 20);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _producerBarrier = _ringBuffer.CreateProducerBarrier(new NoOpConsumer<StubData>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimAndGet() 
        {
            Assert.AreEqual(RingBuffer<StubData>.InitialCursorValue, _ringBuffer.Cursor);

            var expectedEntry = new Entry<StubData>(-1, new StubData(2701));

            StubData oldData;
            var seq = _producerBarrier.NextEntry(out oldData);
            oldData.Value = expectedEntry.Data.Value;
            _producerBarrier.Commit(seq);

            var sequence = _consumerBarrier.WaitFor(0);
            Assert.AreEqual(0L, sequence);

            var entry = _ringBuffer[sequence.Value];
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(0L, _ringBuffer.Cursor);
        }

        [Test]
        public void ShouldClaimAndGetWithTimeout() 
        {
            Assert.AreEqual(RingBuffer<StubData>.InitialCursorValue, _ringBuffer.Cursor);

            var expectedEntry = new Entry<StubData>(-1, new StubData(2701));

            StubData oldData;
            var oldEntry = _producerBarrier.NextEntry(out oldData);
            oldData.Value = expectedEntry.Data.Value;
            _producerBarrier.Commit(oldEntry);

            var sequence = _consumerBarrier.WaitFor(0, TimeSpan.FromMilliseconds(5));
            Assert.AreEqual(0, sequence);

            var entry = _ringBuffer[sequence.Value];
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(0L, _ringBuffer.Cursor);
        }

        [Test]
        public void ShouldGetWithTimeout()
        {
            var sequence = _consumerBarrier.WaitFor(0, TimeSpan.FromMilliseconds(5));
            Assert.AreEqual(RingBuffer<StubData>.InitialCursorValue, sequence);
        }

        [Test]
        public void ShouldClaimAndGetInSeparateThread()
        {
            var messages = GetMessages(0, 0);

            var expectedMessage = new StubData(2701);

            StubData oldData;
            var sequence = _producerBarrier.NextEntry(out oldData);
            oldData.Value = expectedMessage.Value;

            _producerBarrier.Commit(sequence);

            Assert.AreEqual(expectedMessage, messages.Result[0]);
        }

        [Test]
        public void ShouldClaimAndGetMultipleMessages() 
        {
            var numMessages = _ringBuffer.Capacity;
            for (var i = 0; i < numMessages; i++)
            {
                StubData data;
                var entry = _producerBarrier.NextEntry(out data);
                data.Value = i;
                _producerBarrier.Commit(entry);
            }

            var expectedSequence = numMessages - 1;
            var available = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (var i = 0; i < numMessages; i++)
            {
                Assert.AreEqual(i, _ringBuffer[i].Data.Value);
            }
        }

        [Test]
        public void ShouldWrap()
        {
            var numMessages = _ringBuffer.Capacity;
            const int offset = 1000;
            for (var i = 0; i < numMessages + offset ; i++)
            {
                StubData data;
                var sequence = _producerBarrier.NextEntry(out data);
                data.Value = i;
                _producerBarrier.Commit(sequence);
            }

            var expectedSequence = numMessages + offset - 1;
            var available = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (var i = offset; i < numMessages + offset; i++)
            {
                Assert.AreEqual(i, _ringBuffer[i].Data.Value);
            }
        }

        [Test]
        public void ShouldSetAtSpecificSequence()
        {
            const long expectedSequence = 5;
            var forceFillProducerBarrier = _ringBuffer.CreateForceFillProducerBarrier(new NoOpConsumer<StubData>(_ringBuffer));

            var expectedData = forceFillProducerBarrier.ClaimEntry(expectedSequence);
            expectedData.Value = (int)expectedSequence;
            forceFillProducerBarrier.Commit(expectedSequence);

            var sequence = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, sequence);

            var entry = _ringBuffer[sequence.Value].Data;
            Assert.AreEqual(expectedData, entry);

            Assert.AreEqual(expectedSequence, _ringBuffer.Cursor);
        }

        private Task<Gen.List<StubData>> GetMessages(long initial, long toWaitFor)
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
