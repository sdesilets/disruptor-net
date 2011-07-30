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
        private IProducerBarrier<StubData> _producerBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubData>(() => new StubData(-1), 20);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _producerBarrier = _ringBuffer.CreateProducerBarrier(new NoOpConsumer<StubData>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimAndGet()
        {
            Assert.AreEqual(RingBufferConvention.InitialCursorValue, _ringBuffer.Cursor);

            var expectedEntry = new Entry<StubData>(-1, new StubData(2701));

            StubData oldData;
            var seq = _producerBarrier.NextEntry(out oldData);
            oldData.Value = expectedEntry.Data.Value;
            _producerBarrier.Commit(seq);

            var waitForResult = _consumerBarrier.WaitFor(0);
            Assert.AreEqual(0L, waitForResult.AvailableSequence);
            Assert.IsFalse(waitForResult.IsAlerted);

            var entry = _ringBuffer[waitForResult.AvailableSequence];
            Assert.AreEqual(expectedEntry, entry);

            Assert.AreEqual(0L, _ringBuffer.Cursor);
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
            var waitForResult = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, waitForResult.AvailableSequence);

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
            for (var i = 0; i < numMessages + offset; i++)
            {
                StubData data;
                var sequence = _producerBarrier.NextEntry(out data);
                data.Value = i;
                _producerBarrier.Commit(sequence);
            }

            var expectedSequence = numMessages + offset - 1;
            var waitForResult = _consumerBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, waitForResult.AvailableSequence);

            for (var i = offset; i < numMessages + offset; i++)
            {
                Assert.AreEqual(i, _ringBuffer[i].Data.Value);
            }
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

        [Test]
        public void ShouldPreventProducersOvertakingConsumerWrapPoint()
        {
            const int ringBufferSize = 4;
            var mre = new ManualResetEvent(false);
            var producerComplete = false;
            var ringBuffer = new RingBuffer<StubData>(() => new StubData(-1), ringBufferSize);
            var consumer = new TestConsumer(ringBuffer.CreateConsumerBarrier());
            var producerBarrier = ringBuffer.CreateProducerBarrier(consumer);

            var thread = new Thread(() =>
                                        {
                                            for (int i = 0; i <= ringBufferSize; i++) // produce 5 messages
                                            {
                                                StubData data;
                                                long sequence = producerBarrier.NextEntry(out data);
                                                data.Value = i;
                                                producerBarrier.Commit(sequence);

                                                if(i == 3) // unblock main thread after 4th message published
                                                {
                                                    mre.Set();
                                                }
                                            }
                                            
                                            producerComplete = true;
                                        });

            thread.Start();

            mre.WaitOne();
            Assert.AreEqual(ringBufferSize - 1, ringBuffer.Cursor);
            Assert.IsFalse(producerComplete);

            consumer.Run();
            thread.Join();

            Assert.IsTrue(producerComplete);
        }

        private class TestConsumer : IBatchConsumer
        {
            private readonly IConsumerBarrier<StubData> _consumerBarrier;
            private long _sequence = RingBufferConvention.InitialCursorValue;

            public TestConsumer(IConsumerBarrier<StubData> consumerBarrier)
            {
                _consumerBarrier = consumerBarrier;
            }

            public long Sequence
            {
                get { return Interlocked.Read(ref _sequence); }
            }

            public bool Running
            {
                get { return true; }
            }

            public void Halt()
            {
            }

            public void Run()
            {
                _consumerBarrier.WaitFor(0L);
                Interlocked.Increment(ref _sequence);
            }

            public void DelaySequenceWrite(int period)
            {
                
            }
        }
    }
}