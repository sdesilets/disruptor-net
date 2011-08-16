using System;
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
            _dependencyBarrier = _ringBuffer.CreateBarrier();
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
            var stubData = _ringBuffer[0].Data;
            int[] sequenceCounter = {0};
            var handlerOk = false;
            var signalExceptionThrown = false;
            
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
            	          		  try
            	          		  {
	            	          	      _countDownEvent.Signal();
	                                  handlerOk = handlerOk &&(sequenceCounter[0] == 1);
	                                  sequenceCounter[0]++;
            	          		  }
            	          		  catch(InvalidOperationException)
            	          		  {
            	          		  	  signalExceptionThrown = true;
            	          		  	  Assert.Fail("_countDownEvent.Signal should only be called once");
            	          		  }
                              });

            var thread = new Thread(_eventProcessor.Run);
            thread.Start();

            Assert.AreEqual(-1L, _eventProcessor.Sequence);

            StubEvent data;
            var sequence = _ringBuffer.NextEvent(out data);
            _ringBuffer.Commit(sequence);

            _countDownEvent.Wait(50);
            _eventProcessor.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
            Assert.IsTrue(handlerOk);
            Assert.IsFalse(signalExceptionThrown);
        }

        [Test]
        public void ShouldCallMethodsInLifecycleOrderForBatch()
        {
            _batchHandlerMock.Setup(bh => bh.OnAvailable(0, _ringBuffer[0].Data));
            _batchHandlerMock.Setup(bh => bh.OnAvailable(1, _ringBuffer[1].Data));
            _batchHandlerMock.Setup(bh => bh.OnAvailable(2, _ringBuffer[2].Data));
            _batchHandlerMock.Setup(bh => bh.OnEndOfBatch()).Callback(() => _countDownEvent.Signal());

            StubEvent data;
            _ringBuffer.Commit(_ringBuffer.NextEvent(out data));
            _ringBuffer.Commit(_ringBuffer.NextEvent(out data));
            _ringBuffer.Commit(_ringBuffer.NextEvent(out data));

            var thread = new Thread(_eventProcessor.Run);
            thread.Start();

            _countDownEvent.Wait();

            _eventProcessor.Halt();
            thread.Join();

            _batchHandlerMock.VerifyAll();
        }
    }
}