using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.UniCast1P1C
{
    [TestFixture]
    public class UniCast1P1CDisruptorPerfTest : AbstractUniCast1P1CPerfTest
    {
        private readonly RingBuffer<ValueEntry> _ringBuffer;
        private readonly ValueAdditionHandler _handler;
        private readonly IProducerBarrier<ValueEntry> _producerBarrier;

        public UniCast1P1CDisruptorPerfTest()
            : base(1 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEntry>(()=>new ValueEntry(),Size,
                                                     ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                                     WaitStrategyFactory.WaitStrategyOption.Yielding);


            _handler = new ValueAdditionHandler(Iterations);
            _ringBuffer.ConsumeWith(_handler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier();
        }

        public override long RunPass()
        {
            _ringBuffer.StartConsumers();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                ValueEntry data;
                var sequence = _producerBarrier.NextEntry(out data);

                data.Value = i;

                _producerBarrier.Commit(sequence);
            }

            while (!_handler.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            _ringBuffer.Halt();

            Assert.AreEqual(ExpectedResult, _handler.Value.Value, "RunDisruptorPass");

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}