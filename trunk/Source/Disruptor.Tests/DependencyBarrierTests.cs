using System;
using System.Threading;
using Disruptor.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class DependencyBarrierTests
    {
        private RingBuffer<StubData> _ringBuffer;
        private Mock<IEventProcessor> _eventProcessorMock1;
        private Mock<IEventProcessor> _eventProcessorMock2;
        private Mock<IEventProcessor> _eventProcessorMock3;
        private IDependencyBarrier<StubData> _dependencyBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubData>(()=>new StubData(-1), 64);

            _eventProcessorMock1 = new Mock<IEventProcessor>();
            _eventProcessorMock2 = new Mock<IEventProcessor>();
            _eventProcessorMock3 = new Mock<IEventProcessor>();

            _dependencyBarrier = _ringBuffer.CreateBarrier(_eventProcessorMock1.Object, _eventProcessorMock2.Object, _eventProcessorMock3.Object);
            _ringBuffer.SetTrackedEventProcessors(new NoOpEventProcessor<StubData>(_ringBuffer));
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsAhead() 
        {
            const int expectedNumberEvents = 10;
            const int expectedWorkSequence = 9;
            FillRingBuffer(expectedNumberEvents);

            _eventProcessorMock1.SetupGet(c => c.Sequence).Returns(expectedNumberEvents);
            _eventProcessorMock2.SetupGet(c => c.Sequence).Returns(expectedNumberEvents);
            _eventProcessorMock3.SetupGet(c => c.Sequence).Returns(expectedNumberEvents);

            var waitForResult = _dependencyBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);

            _eventProcessorMock1.Verify();
            _eventProcessorMock2.Verify();
            _eventProcessorMock3.Verify();
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereAllWorkersAreBlockedOnRingBuffer() 
        {
            const long expectedNumberEvents = 10;
            FillRingBuffer(expectedNumberEvents);

            var workers = new StubEventProcessor[3];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = new StubEventProcessor(expectedNumberEvents - 1);
            }

            var eventProcessorBarrier = _ringBuffer.CreateBarrier(workers);

            new Thread(() =>
                    {
                        StubData data;
                        var sequence = _ringBuffer.NextEvent(out data);
                        data.Value = (int)sequence;
                        _ringBuffer.Commit(sequence);

                        foreach (var stubWorker in workers)
                        {
                            stubWorker.Sequence = sequence;
                        }
                    })
                    .Start();

            const long expectedWorkSequence = expectedNumberEvents;
            var waitForResult = eventProcessorBarrier.WaitFor(expectedNumberEvents);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);
        }

        [Test]
        public void ShouldInterruptDuringBusySpin()
        {
            const long expectedNumberEvents = 10;
            FillRingBuffer(expectedNumberEvents);

            var countdownEvent = new CountdownEvent(9);

            _eventProcessorMock1.SetupGet(c => c.Sequence).Returns(8L).Callback(() => countdownEvent.Signal());
            _eventProcessorMock2.SetupGet(c => c.Sequence).Returns(8L).Callback(() => countdownEvent.Signal());
            _eventProcessorMock3.SetupGet(c => c.Sequence).Returns(8L).Callback(() =>
                                                                              {
                                                                                  countdownEvent.Signal();
                                                                                  Thread.Sleep(100); // wait a bit to prevent race (otherwise the WaitStrategy 
                                                                                  // does another iterations which decrease the countdown event below 0
                                                                              });

            var alerted = new[] { false };
            var t = new Thread(() =>
            {
            	if(_dependencyBarrier.WaitFor(expectedNumberEvents - 1).IsAlerted)
            		alerted[0] = true;
            });

            t.Start();
            Assert.IsTrue(countdownEvent.Wait(TimeSpan.FromMilliseconds(100000000)));
            _dependencyBarrier.Alert();
            t.Join();

            Assert.IsTrue(alerted[0], "Thread was not interrupted");

            _eventProcessorMock1.Verify();
            _eventProcessorMock2.Verify();
            _eventProcessorMock3.Verify();
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsBehind() 
        {
            const long expectedNumberEvents = 10;
            FillRingBuffer(expectedNumberEvents);

            var eventProcessors = new StubEventProcessor[3];
            for (var i = 0; i < eventProcessors.Length; i++)
            {
                eventProcessors[i] = new StubEventProcessor(expectedNumberEvents - 2);
            }

            var eventProcessorBarrier = _ringBuffer.CreateBarrier(eventProcessors);

            new Thread(()=>
                           {
                                foreach (var stubWorker in eventProcessors)
                                {
                                    stubWorker.Sequence = stubWorker.Sequence + 1;
                                }
                           }).Start();

            const long expectedWorkSequence = expectedNumberEvents - 1;
            var waitForResult = eventProcessorBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);
        }

        [Test]
        public void ShouldSetAndClearAlertStatus()
        {
            Assert.IsFalse(_dependencyBarrier.IsAlerted);

            _dependencyBarrier.Alert();
            Assert.IsTrue(_dependencyBarrier.IsAlerted);

            _dependencyBarrier.ClearAlert();
            Assert.IsFalse(_dependencyBarrier.IsAlerted);
        }

        private void FillRingBuffer(long expectedNumberEvents)
        {
            for (var i = 0; i < expectedNumberEvents; i++)
            {
                StubData data;
                var sequence = _ringBuffer.NextEvent(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }
        }

        private class StubEventProcessor : IEventProcessor
        {
            public StubEventProcessor(long sequence)
            {
                _sequence = sequence;
            }

            private long _sequence;

            public void Run()
            {
            }

            public void DelaySequenceWrite(int period)
            {
                
            }

            public long Sequence
            {
                get
                {
                    return Thread.VolatileRead(ref _sequence);
                }
                set
                {
                    Thread.VolatileWrite(ref _sequence, value);
                }
            }

            public bool Running
            {
                get { return true; }
            }

            public void Halt()
            {
            }
        }
    }
}