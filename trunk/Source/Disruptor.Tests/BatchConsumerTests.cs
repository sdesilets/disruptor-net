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
        private RingBuffer<StubData> _ringBuffer;
        private IConsumerBarrier<StubData> _consumerBarrier;
        private Mock<IBatchHandler<StubData>> _batchHandlerMock;
        private BatchConsumer<StubData> _batchConsumer;
        private CountdownEvent _countDownEvent;
        private IReferenceTypeProducerBarrier<StubData> _producerBarrier;

        [SetUp]
        public void Setup()
        {
            _ringBuffer = new RingBuffer<StubData>(()=>new StubData(-1), 16);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _batchHandlerMock = new Mock<IBatchHandler<StubData>>();
            _countDownEvent = new CountdownEvent(1);
            _batchConsumer = new BatchConsumer<StubData>(_consumerBarrier, _batchHandlerMock.Object);
            
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
            var stubData = _ringBuffer[0].Data;
            int[] sequenceCounter = {0};
            var handlerOk = false;

            //TODO refactor with Moq.Sequence
            _batchHandlerMock.Setup(bh => bh.OnAvailable(0, stubData))
                .Callback(()=>
                              {
                                  handlerOk = (sequenceCounter[0] == 0);
                                  sequenceCounter[0]++;
                              });
            _batchHandlerMock.Setup(bh => bh.OnEndOfBatch())
                .Callback(() =>
                              {
                                  _countDownEvent.Signal();
                                  handlerOk = handlerOk &&(sequenceCounter[0] == 1);
                                  sequenceCounter[0]++;
                              });

            var thread = new Thread(_batchConsumer.Run);
            thread.Start();

            Assert.AreEqual(-1L, _batchConsumer.Sequence);

            StubData data;
            var sequence = _producerBarrier.NextEntry(out data);
            _producerBarrier.Commit(sequence);

            _countDownEvent.Wait(50);
            _batchConsumer.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
            Assert.IsTrue(handlerOk);
        }

        [Test]
        public void ShouldCallMethodsInLifecycleOrderForBatch()
        {
            _batchHandlerMock.Setup(bh => bh.OnAvailable(0, _ringBuffer[0].Data));
            _batchHandlerMock.Setup(bh => bh.OnAvailable(1, _ringBuffer[1].Data));
            _batchHandlerMock.Setup(bh => bh.OnAvailable(2, _ringBuffer[2].Data));
            _batchHandlerMock.Setup(bh => bh.OnEndOfBatch()).Callback(() => _countDownEvent.Signal());

            StubData data;
            _producerBarrier.Commit(_producerBarrier.NextEntry(out data));
            _producerBarrier.Commit(_producerBarrier.NextEntry(out data));
            _producerBarrier.Commit(_producerBarrier.NextEntry(out data));

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
            var exceptionHandlerMock = new Mock<IExceptionHandler<StubData>>();

            _batchConsumer.SetExceptionHandler(exceptionHandlerMock.Object);

            _batchHandlerMock.Setup(bh => bh.OnAvailable(0, _ringBuffer[0].Data))
                             .Callback(() => { throw ex; });
            exceptionHandlerMock.Setup(handler => handler.Handle(ex, It.IsAny<Entry<StubData>>()))
                             .Callback(() => _countDownEvent.Signal());

            var thread = new Thread(_batchConsumer.Run);
            thread.Start();

            StubData data;
            var sequence = _producerBarrier.NextEntry(out data);
            _producerBarrier.Commit(sequence);

            _countDownEvent.Wait();

            _batchConsumer.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
            exceptionHandlerMock.VerifyAll();
        }
    }
}