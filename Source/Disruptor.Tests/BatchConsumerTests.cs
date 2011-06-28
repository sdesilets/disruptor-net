using System;
using System.Threading;
using Disruptor.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class BatchConsumerTests
    {
        private RingBuffer<StubEntry> _ringBuffer;
        private IConsumerBarrier<StubEntry> _consumerBarrier;
        private Mock<IBatchHandler<StubEntry>> _batchHandlerMock;
        private BatchConsumer<StubEntry> _batchConsumer;
        private CountdownEvent _countDownEvent;
        private IProducerBarrier<StubEntry> _producerBarrier;

        [SetUp]
        public void Setup()
        {
            _ringBuffer = new RingBuffer<StubEntry>(()=>new StubEntry(-1), 16);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _batchHandlerMock = new Mock<IBatchHandler<StubEntry>>();
            _batchConsumer = new BatchConsumer<StubEntry>(_consumerBarrier, _batchHandlerMock.Object);
            _countDownEvent = new CountdownEvent(1);
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumer);
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(ArgumentNullException))]
        public void ShouldThrowExceptionOnSettingNullExceptionHandler()
        {
            _batchConsumer.SetExceptionHandler(null);
        }

        [Test]
        public void ShouldReturnUnderlyingBarrier()
        {
            Assert.AreSame(_consumerBarrier, _batchConsumer.ConsumerBarrier);
        }

        [Test]
        public void ShouldCallMethodsInLifecycleOrder()
        {
            _batchHandlerMock.Setup(bh => bh.OnAvailable(_ringBuffer.GetEntry(0)));
            _batchHandlerMock.Setup(bh => bh.OnEndOfBatch()).Callback(() => _countDownEvent.Signal());
            _batchHandlerMock.Setup(bh => bh.OnCompletion());
            
            var thread = new Thread(_batchConsumer.Run);
            thread.Start();

            Assert.AreEqual(-1L, _batchConsumer.Sequence);

            _producerBarrier.Commit(_producerBarrier.NextEntry());

            _countDownEvent.Wait(50);

            _batchConsumer.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
        }

        [Test]
        public void ShouldCallMethodsInLifecycleOrderForBatch()
        {
            _batchHandlerMock.Setup(bh => bh.OnAvailable(_ringBuffer.GetEntry(0)));
            _batchHandlerMock.Setup(bh => bh.OnAvailable(_ringBuffer.GetEntry(1)));
            _batchHandlerMock.Setup(bh => bh.OnAvailable(_ringBuffer.GetEntry(2)));
            _batchHandlerMock.Setup(bh => bh.OnEndOfBatch()).Callback(() => _countDownEvent.Signal());
            _batchHandlerMock.Setup(bh => bh.OnCompletion());

            _producerBarrier.Commit(_producerBarrier.NextEntry());
            _producerBarrier.Commit(_producerBarrier.NextEntry());
            _producerBarrier.Commit(_producerBarrier.NextEntry());

            var thread = new Thread(_batchConsumer.Run);
            thread.Start();

            _countDownEvent.Wait();

            _batchConsumer.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
        }

        [Test]
        public void ShouldCallExceptionHandlerOnUncaughtException()
        {
            var ex = new Exception();
            var exceptionHandlerMock = new Mock<IExceptionHandler>();

            _batchConsumer.SetExceptionHandler(exceptionHandlerMock.Object);

            _batchHandlerMock.Setup(bh => bh.OnAvailable(_ringBuffer.GetEntry(0)))
                             .Callback(() => { throw ex; });
            exceptionHandlerMock.Setup(handler => handler.Handle(ex, _ringBuffer.GetEntry(0)))
                             .Callback(() => _countDownEvent.Signal());
            _batchHandlerMock.Setup(bh => bh.OnCompletion());

            var thread = new Thread(_batchConsumer.Run);
            thread.Start();

            _producerBarrier.Commit(_producerBarrier.NextEntry());

            _countDownEvent.Wait();

            _batchConsumer.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
            exceptionHandlerMock.VerifyAll();
        }
    }
}