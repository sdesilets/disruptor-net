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
        private IProducerBarrier<StubData> _producerBarrier;

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
            Assert.IsFalse(signalExceptionThrown);
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

        //[Test]
        //public void ShouldCallOnAvailableAction()
        //{
        //    const string expectedString = "Foo";
        //    const int expectedValue = 32;

        //    long sequenceRecieved = -1;
        //    StubData receivedData = null;
        //    var mru = new ManualResetEvent(false);
        //    var batchConsumer = new BatchConsumer<StubData>(_consumerBarrier, (sequence, data) =>
        //                                                                          {
        //                                                                              sequenceRecieved = sequence;
        //                                                                              receivedData = data;
        //                                                                              mru.Set();
        //                                                                          });
        //    _producerBarrier = _ringBuffer.CreateProducerBarrier(batchConsumer);

        //    var thread = new Thread(batchConsumer.Run);
        //    thread.Start();

        //    StubData expectedData;
        //    var s = _producerBarrier.NextEntry(out expectedData);
        //    expectedData.TestString = expectedString;
        //    expectedData.Value = expectedValue;
        //    _producerBarrier.Commit(s);

        //    mru.WaitOne();

        //    Assert.AreEqual(0, sequenceRecieved);
        //    Assert.AreEqual(expectedString, receivedData.TestString);
        //    Assert.AreEqual(expectedValue, receivedData.Value);

        //    batchConsumer.Halt();
        //    thread.Join();
        //}

        //[Test]
        //public void ShouldCallOnBatchCompleteAction()
        //{
        //    var onEndOfBatchWasCalled = false;
        //    var mru = new ManualResetEvent(false);
        //    var batchConsumer = new BatchConsumer<StubData>(_consumerBarrier, (sequence, data) => { },
        //                                                    () =>
        //                                                        {
        //                                                            onEndOfBatchWasCalled = true;
        //                                                            mru.Set();
        //                                                        });
        //    _producerBarrier = _ringBuffer.CreateProducerBarrier(batchConsumer);

        //    var thread = new Thread(batchConsumer.Run);
        //    thread.Start();

        //    StubData expectedData;
        //    var s = _producerBarrier.NextEntry(out expectedData);
        //    _producerBarrier.Commit(s);

        //    mru.WaitOne();

        //    Assert.IsTrue(onEndOfBatchWasCalled);

        //    batchConsumer.Halt();
        //    thread.Join();
        //}
    }
}