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
        private RingBuffer<StubEvent> _ringBuffer;
        private IDependencyBarrier _dependencyBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubEvent>(() => new StubEvent(-1), 20);
            _dependencyBarrier = _ringBuffer.CreateBarrier();
            _ringBuffer.SetTrackedEventProcessors(new NoOpEventProcessor<StubEvent>(_ringBuffer));
        }

        [Test]
        public void ShouldClaimAndGet()
        {
            Assert.AreEqual(RingBufferConvention.InitialCursorValue, _ringBuffer.Cursor);

            var expectedEvent = new Event<StubEvent>(-1, new StubEvent(2701));

            StubEvent oldEvent;
            var seq = _ringBuffer.NextEvent(out oldEvent);
            oldEvent.Value = expectedEvent.Data.Value;
            _ringBuffer.Commit(seq);

            var waitForResult = _dependencyBarrier.WaitFor(0);
            Assert.AreEqual(0L, waitForResult.AvailableSequence);
            Assert.IsFalse(waitForResult.IsAlerted);

            var entry = _ringBuffer[waitForResult.AvailableSequence];
            Assert.AreEqual(expectedEvent, entry);

            Assert.AreEqual(0L, _ringBuffer.Cursor);
        }

        [Test]
        public void ShouldClaimAndGetInSeparateThread()
        {
            var events = GetEvents(0, 0);

            var expectedEvent = new StubEvent(2701);

            StubEvent oldEvent;
            var sequence = _ringBuffer.NextEvent(out oldEvent);
            oldEvent.Value = expectedEvent.Value;

            _ringBuffer.Commit(sequence);

            Assert.AreEqual(expectedEvent, events.Result[0]);
        }

        [Test]
        public void ShouldClaimAndGetMultipleEvents()
        {
            var numEvents = _ringBuffer.Capacity;
            for (var i = 0; i < numEvents; i++)
            {
                StubEvent data;
                var sequence = _ringBuffer.NextEvent(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }

            var expectedSequence = numEvents - 1;
            var waitForResult = _dependencyBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, waitForResult.AvailableSequence);

            for (var i = 0; i < numEvents; i++)
            {
                Assert.AreEqual(i, _ringBuffer[i].Data.Value);
            }
        }

        [Test]
        public void ShouldWrap()
        {
            var numEvents = _ringBuffer.Capacity;
            const int offset = 1000;
            for (var i = 0; i < numEvents + offset; i++)
            {
                StubEvent data;
                var sequence = _ringBuffer.NextEvent(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }

            var expectedSequence = numEvents + offset - 1;
            var waitForResult = _dependencyBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, waitForResult.AvailableSequence);

            for (var i = offset; i < numEvents + offset; i++)
            {
                Assert.AreEqual(i, _ringBuffer[i].Data.Value);
            }
        }

        private Task<Gen.List<StubEvent>> GetEvents(long initial, long toWaitFor)
        {
            var barrier = new Barrier(2);
            var dependencyBarrier = _ringBuffer.CreateBarrier();

            var testWaiter = new TestWaiter(barrier, dependencyBarrier, _ringBuffer, initial, toWaitFor);
            var task = Task.Factory.StartNew(() => testWaiter.Call());

            barrier.SignalAndWait();

            return task;
        }

        [Test]
        public void ShouldPreventProducersOvertakingEventProcessorsWrapPoint()
        {
            const int ringBufferSize = 4;
            var mre = new ManualResetEvent(false);
            var producerComplete = false;
            var ringBuffer = new RingBuffer<StubEvent>(() => new StubEvent(-1), ringBufferSize);
            var processor = new TestEventProcessor(ringBuffer.CreateBarrier());
            ringBuffer.SetTrackedEventProcessors(processor);

            var thread = new Thread(() =>
                                        {
                                            for (int i = 0; i <= ringBufferSize; i++) // produce 5 events
                                            {
                                                StubEvent data;
                                                long sequence = ringBuffer.NextEvent(out data);
                                                data.Value = i;
                                                ringBuffer.Commit(sequence);

                                                if(i == 3) // unblock main thread after 4th event published
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

            processor.Run();
            thread.Join();

            Assert.IsTrue(producerComplete);
        }

        private class TestEventProcessor : IEventProcessor
        {
            private readonly IDependencyBarrier _dependencyBarrier;
            private readonly Sequence _sequence = new Sequence(RingBufferConvention.InitialCursorValue);

            public TestEventProcessor(IDependencyBarrier dependencyBarrier)
            {
                _dependencyBarrier = dependencyBarrier;
            }

            public bool Running
            {
                get { return true; }
            }

            public Sequence Sequence
            {
                get { return _sequence; }
            }

            public void Halt()
            {
            }

            public void Run()
            {
                _dependencyBarrier.WaitFor(0L);
                _sequence.Value += 1;
            }

            public void DelaySequenceWrite(int period)
            {
                
            }
        }
    }
}