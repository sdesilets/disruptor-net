using System;
using System.Threading;
using Disruptor.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class ConsumerBarrierTests
    {
        private RingBuffer<StubEntry> _ringBuffer;
        private Mock<IConsumer> _consumerMock1;
        private Mock<IConsumer> _consumerMock2;
        private Mock<IConsumer> _consumerMock3;
        private IConsumerBarrier<StubEntry> _consumerBarrier;
        private IProducerBarrier<StubEntry> _producerBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubEntry>(()=>new StubEntry(-1), 64);

            _consumerMock1 = new Mock<IConsumer>();
            _consumerMock2 = new Mock<IConsumer>();
            _consumerMock3 = new Mock<IConsumer>();

            _consumerBarrier = _ringBuffer.CreateConsumerBarrier(_consumerMock1.Object, _consumerMock2.Object, _consumerMock3.Object);
            _producerBarrier = _ringBuffer.CreateProducerBarrier(new NoOpConsumer<StubEntry>(_ringBuffer));
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsAhead() 
        {
            const int expectedNumberMessages = 10;
            const int expectedWorkSequence = 9;
            FillRingBuffer(expectedNumberMessages);

            _consumerMock1.SetupGet(c => c.Sequence).Returns(expectedNumberMessages);
            _consumerMock2.SetupGet(c => c.Sequence).Returns(expectedNumberMessages);
            _consumerMock3.SetupGet(c => c.Sequence).Returns(expectedNumberMessages);

            var completedWorkSequence = _consumerBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);

            _consumerMock1.Verify();
            _consumerMock2.Verify();
            _consumerMock3.Verify();
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereAllWorkersAreBlockedOnRingBuffer() 
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var workers = new StubConsumer[3];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = new StubConsumer(expectedNumberMessages - 1);
            }

            var consumerBarrier = _ringBuffer.CreateConsumerBarrier(workers);

            new Thread(() =>
                    {
                        var entry = _producerBarrier.NextEntry();
                        entry.Value = (int) entry.Sequence;
                        _producerBarrier.Commit(entry);

                        foreach (var stubWorker in workers)
                        {
                            stubWorker.Sequence = entry.Sequence;
                        }
                    })
                    .Start();

            const long expectedWorkSequence = expectedNumberMessages;
            var completedWorkSequence = consumerBarrier.WaitFor(expectedNumberMessages);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
        }

        [Test]
        [Ignore("Test to review, not working ATM")] //TODO
        public void ShouldInterruptDuringBusySpin()
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var countdownEvent = new CountdownEvent(9);

            _consumerMock1.SetupGet(c => c.Sequence).Returns(8L).Callback(() => countdownEvent.Signal());
            _consumerMock2.SetupGet(c => c.Sequence).Returns(8L).Callback(() => countdownEvent.Signal());
            _consumerMock3.SetupGet(c => c.Sequence).Returns(8L).Callback(() => countdownEvent.Signal());

            var alerted = new[] { false };
            var t = new Thread(() =>
            {
                    try
                    {
                        _consumerBarrier.WaitFor(expectedNumberMessages - 1);
                    }
                    catch (AlertException)
                    {
                        alerted[0] = true;
                    }
                    //catch (InterruptedException e) // TODO
                    //{
                    //    // don't care
                    //}
            });

            t.Start();
            Assert.IsTrue(countdownEvent.Wait(TimeSpan.FromMilliseconds(1000)));
            _consumerBarrier.Alert();
            t.Join();

            Assert.IsTrue(alerted[0], "Thread was not interrupted");

            _consumerMock1.Verify();
            _consumerMock2.Verify();
            _consumerMock3.Verify();
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsBehind() 
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var entryConsumers = new StubConsumer[3];
            for (var i = 0; i < entryConsumers.Length; i++)
            {
                entryConsumers[i] = new StubConsumer(expectedNumberMessages - 2);
            }

            var consumerBarrier = _ringBuffer.CreateConsumerBarrier(entryConsumers);

            new Thread(()=>
                           {
                                foreach (var stubWorker in entryConsumers)
                                {
                                    stubWorker.Sequence = stubWorker.Sequence + 1;
                                }
                           }).Start();

            const long expectedWorkSequence = expectedNumberMessages - 1;
            var completedWorkSequence = consumerBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(completedWorkSequence >= expectedWorkSequence);
        }

        [Test]
        public void ShouldSetAndClearAlertStatus()
        {
            Assert.IsFalse(_consumerBarrier.IsAlerted);

            _consumerBarrier.Alert();
            Assert.IsTrue(_consumerBarrier.IsAlerted);

            _consumerBarrier.ClearAlert();
            Assert.IsFalse(_consumerBarrier.IsAlerted);
        }

        private void FillRingBuffer(long expectedNumberMessages)
        {
            for (var i = 0; i < expectedNumberMessages; i++)
            {
                var entry = _producerBarrier.NextEntry();
                entry.Value = (int)i;
                _producerBarrier.Commit(entry);
            }
        }

        private class StubConsumer : IConsumer
        {
            public StubConsumer(long sequence)
            {
                _sequence = sequence;
            }

            private long _sequence;

            public void Run()
            {
            }

            public long Sequence
            {
                get { return Thread.VolatileRead(ref _sequence); }
                set{ Thread.VolatileWrite(ref _sequence, value);}
            }

            public void Halt()
            {
            }
        }
    }
}