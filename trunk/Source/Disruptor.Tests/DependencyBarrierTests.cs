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
        private RingBuffer<StubEvent> _ringBuffer;
        private Mock<IEventProcessor> _eventProcessorMock1;
        private Mock<IEventProcessor> _eventProcessorMock2;
        private Mock<IEventProcessor> _eventProcessorMock3;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubEvent>(()=>new StubEvent(-1), 64);

            _eventProcessorMock1 = new Mock<IEventProcessor>();
            _eventProcessorMock2 = new Mock<IEventProcessor>();
            _eventProcessorMock3 = new Mock<IEventProcessor>();

            _ringBuffer.SetTrackedEventProcessors(new NoOpEventProcessor<StubEvent>(_ringBuffer));
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsAhead() 
        {
            const int expectedNumberEvents = 10;
            const int expectedWorkSequence = 9;
            FillRingBuffer(expectedNumberEvents);

            var sequence1 = new Sequence(expectedNumberEvents);
            var sequence2 = new Sequence(expectedWorkSequence);
            var sequence3 = new Sequence(expectedNumberEvents);

            _eventProcessorMock1.SetupGet(c => c.Sequence).Returns(sequence1);
            _eventProcessorMock2.SetupGet(c => c.Sequence).Returns(sequence2);
            _eventProcessorMock3.SetupGet(c => c.Sequence).Returns(sequence3);

            var dependencyBarrier = _ringBuffer.CreateDependencyBarrier(_eventProcessorMock1.Object, _eventProcessorMock2.Object, _eventProcessorMock3.Object);

            var waitForResult = dependencyBarrier.WaitFor(expectedWorkSequence);
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

            var dependencyBarrier = _ringBuffer.CreateDependencyBarrier(workers);

            new Thread(() =>
                    {
                        StubEvent data;
                        var sequence = _ringBuffer.NextEvent(out data);
                        data.Value = (int)sequence;
                        _ringBuffer.Publish(sequence);

                        foreach (var stubWorker in workers)
                        {
                            stubWorker.Sequence.Value = sequence;
                        }
                    })
                    .Start();

            const long expectedWorkSequence = expectedNumberEvents;
            var waitForResult = dependencyBarrier.WaitFor(expectedNumberEvents);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);
        }

        [Test]
        public void ShouldInterruptDuringBusySpin()
        {
            const long expectedNumberEvents = 10;
            FillRingBuffer(expectedNumberEvents);

            var sequence1 = new Sequence(8L);
            var sequence2 = new Sequence(8L);
            var sequence3 = new Sequence(8L);

            _eventProcessorMock1.SetupGet(c => c.Sequence).Returns(sequence1);
            _eventProcessorMock2.SetupGet(c => c.Sequence).Returns(sequence2);
            _eventProcessorMock3.SetupGet(c => c.Sequence).Returns(sequence3);

            var dependencyBarrier = _ringBuffer.CreateDependencyBarrier(_eventProcessorMock1.Object, _eventProcessorMock2.Object, _eventProcessorMock3.Object);

            var alerted = new[] { false };
            var t = new Thread(() =>
            {
            	if(dependencyBarrier.WaitFor(expectedNumberEvents - 1).IsAlerted)
            		alerted[0] = true;
            });

            t.Start();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            dependencyBarrier.Alert();
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

            var eventProcessorBarrier = _ringBuffer.CreateDependencyBarrier(eventProcessors);

            new Thread(()=>
                           {
                                foreach (var stubWorker in eventProcessors)
                                {
                                    stubWorker.Sequence.Value += 1;
                                }
                           }).Start();

            const long expectedWorkSequence = expectedNumberEvents - 1;
            var waitForResult = eventProcessorBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);
        }

        [Test]
        public void ShouldSetAndClearAlertStatus()
        {
            var dependencyBarrier = _ringBuffer.CreateDependencyBarrier();
            Assert.IsFalse(dependencyBarrier.IsAlerted);

            dependencyBarrier.Alert();
            Assert.IsTrue(dependencyBarrier.IsAlerted);

            dependencyBarrier.ClearAlert();
            Assert.IsFalse(dependencyBarrier.IsAlerted);
        }

        private void FillRingBuffer(long expectedNumberEvents)
        {
            for (var i = 0; i < expectedNumberEvents; i++)
            {
                StubEvent data;
                var sequence = _ringBuffer.NextEvent(out data);
                data.Value = i;
                _ringBuffer.Publish(sequence);
            }
        }

        private class StubEventProcessor : IEventProcessor
        {
            private readonly Sequence _sequence = new Sequence(RingBufferConvention.InitialCursorValue);

            public StubEventProcessor(long sequence)
            {
                _sequence.Value = sequence;
            }

            public void Run()
            {
            }

            public void DelaySequenceWrite(int period)
            {
                
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
        }
    }
}