using System.Threading;
using Disruptor.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class EventProcessorTests
    {
        private RingBuffer<StubEvent> _ringBuffer;
        private IDependencyBarrier _dependencyBarrier;
        private Mock<IEventHandler<StubEvent>> _batchHandlerMock;
        private EventProcessor<StubEvent> _eventProcessor;
        private CountdownEvent _countDownEvent;

        [SetUp]
        public void Setup()
        {
            _ringBuffer = new RingBuffer<StubEvent>(()=>new StubEvent(-1), 16);
            _dependencyBarrier = _ringBuffer.CreateDependencyBarrier();
            _batchHandlerMock = new Mock<IEventHandler<StubEvent>>();
            _countDownEvent = new CountdownEvent(1);
            _eventProcessor = new EventProcessor<StubEvent>(_ringBuffer, _dependencyBarrier, _batchHandlerMock.Object);
        }

        [Test]
        public void ShouldReturnUnderlyingBarrier()
        {
            Assert.AreSame(_dependencyBarrier, _eventProcessor.DependencyBarrier);
        }

        [Test]
        public void ShouldCallMethodsInLifecycleOrder()
        {
            _batchHandlerMock.Setup(bh => bh.OnNext(0, _ringBuffer[0].Data, true))
                             .Callback(() => _countDownEvent.Signal());

            var thread = new Thread(_eventProcessor.Run);
            thread.Start();

            Assert.AreEqual(-1L, _eventProcessor.Sequence.Value);

            _ringBuffer.Publish(_ringBuffer.NextEvent());

            _countDownEvent.Wait(50);
            _eventProcessor.Halt();
            thread.Join();

            _batchHandlerMock.Verify(bh => bh.OnNext(0, _ringBuffer[0].Data, true), Times.Once());
        }

        [Test]
        public void ShouldCallMethodsInLifecycleOrderForBatch()
        {
            _batchHandlerMock.Setup(bh => bh.OnNext(0, _ringBuffer[0].Data, false));
            _batchHandlerMock.Setup(bh => bh.OnNext(1, _ringBuffer[1].Data, false));
            _batchHandlerMock.Setup(bh => bh.OnNext(2, _ringBuffer[2].Data, true)).Callback(() => _countDownEvent.Signal());

            _ringBuffer.Publish(_ringBuffer.NextEvent());
            _ringBuffer.Publish(_ringBuffer.NextEvent());
            _ringBuffer.Publish(_ringBuffer.NextEvent());

            var thread = new Thread(_eventProcessor.Run);
            thread.Start();

            _countDownEvent.Wait();

            _eventProcessor.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
        }
    }
}