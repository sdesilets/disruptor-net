using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.UniCast1P1CBatch
{
    [TestFixture]
    public class UniCast1P1CBatchDisruptorPerfTest:AbstractUniCast1P1CBatchPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;
        private readonly ValueAdditionEventHandler _eventHandler;

        public UniCast1P1CBatchDisruptorPerfTest()
            : base(100 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEvent>(()=>new ValueEvent(), Size,
                                   ClaimStrategyOption.SingleProducer,
                                   WaitStrategyOption.Yielding);

            _eventHandler = new ValueAdditionEventHandler(Iterations);
            _ringBuffer.ProcessWith(_eventHandler);
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }

        public override long RunPass()
        {
            _ringBuffer.StartProcessors();

            const int batchSize = 10;

            var sw = Stopwatch.StartNew();

            long offset = 0;
            for (long i = 0; i < Iterations; i += batchSize)
            {
                var sequenceBatch = _ringBuffer.NextEvents(batchSize);
                for (long sequence = sequenceBatch.Start; sequence <= sequenceBatch.End; sequence++)
                {
                    ValueEvent valueEvent = _ringBuffer.GetEvent(sequence);
                    valueEvent.Value = offset++;
                }
                _ringBuffer.Publish(sequenceBatch);
            }

            while (!_eventHandler.Done)
            {
                // busy spin
            }

            long opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _ringBuffer.Halt();

            Assert.AreEqual(ExpectedResult, _eventHandler.Value.Value);

            return opsPerSecond;
        }
    }
}