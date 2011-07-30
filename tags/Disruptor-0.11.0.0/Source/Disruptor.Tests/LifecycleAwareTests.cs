using System;
using System.Threading;
using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class LifecycleAwareTests
    {
        private readonly ManualResetEvent _startMru = new ManualResetEvent(false);
        private readonly ManualResetEvent _shutdownMru = new ManualResetEvent(false);
        private readonly RingBuffer<StubData> _ringBuffer = new RingBuffer<StubData>(()=>new StubData(-1), 16);
        private IConsumerBarrier<StubData> _consumerBarrier;
        private LifecycleAwareBatchHandler _handler;
        private BatchConsumer<StubData> _batchConsumer;

        [SetUp]
        public void SetUp()
        {
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _handler = new LifecycleAwareBatchHandler(_startMru, _shutdownMru);
            _batchConsumer = new BatchConsumer<StubData>(_consumerBarrier, _handler);
        }

        [Test]
        public void ShouldNotifyOfBatchConsumerLifecycle()
        {
            new Thread(_batchConsumer.Run).Start();

            _startMru.WaitOne();

            _batchConsumer.Halt();

            _shutdownMru.WaitOne();

            Assert.AreEqual(_handler.StartCounter, 1);
            Assert.AreEqual(_handler.ShutdownCounter, 1);
        }
        
        private sealed class LifecycleAwareBatchHandler : IBatchHandler<StubData>, ILifecycleAware
        {
            private readonly ManualResetEvent _startMru;
            private readonly ManualResetEvent _shutdownMru;
            private int _startCounter;
            private int _shutdownCounter;

            public int StartCounter
            {
                get { return _startCounter; }
            }

            public int ShutdownCounter
            {
                get { return _shutdownCounter; }
            }

            public LifecycleAwareBatchHandler(ManualResetEvent startMru, ManualResetEvent shutdownMru)
            {
                _startMru = startMru;
                _shutdownMru = shutdownMru;
            }

            public void OnAvailable(long sequence, StubData data)
            {
            }

            public void OnEndOfBatch()
            {
            }

            public void OnStart()
            {
                ++_startCounter;
                _startMru.Set();
            }

            public void OnStop()
            {
                ++_shutdownCounter;
                _shutdownMru.Set();
            }
        }
    }    
}
