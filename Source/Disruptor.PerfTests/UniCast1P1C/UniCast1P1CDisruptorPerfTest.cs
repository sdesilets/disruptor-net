using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.UniCast1P1C
{
    [TestFixture]
    public class UniCast1P1CDisruptorPerfTest : AbstractUniCast1P1CPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;
        private readonly ValueAdditionEventHandler _eventHandler;

        public UniCast1P1CDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEvent>(()=>new ValueEvent(),Size,
                                                     ClaimStrategyOption.SingleProducer,
                                                     WaitStrategyOption.Yielding);


            _eventHandler = new ValueAdditionEventHandler(Iterations);
            _ringBuffer.ProcessWith(_eventHandler);
        }

        public override long RunPass()
        {
            _ringBuffer.StartProcessors();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                ValueEvent data;
                var sequence = _ringBuffer.NextEvent(out data);

                data.Value = i;

                _ringBuffer.Publish(sequence);
            }

            while (!_eventHandler.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _ringBuffer.Halt();

            Assert.AreEqual(ExpectedResult, _eventHandler.Value.Value, "RunDisruptorPass");

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}