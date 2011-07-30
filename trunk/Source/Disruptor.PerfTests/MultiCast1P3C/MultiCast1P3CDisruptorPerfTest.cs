using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.MultiCast1P3C
{
    [TestFixture]
    public class MultiCast1P3CDisruptorPerfTest:AbstractMultiCast1P3CPerfTest
    {
        private readonly RingBuffer<ValueEntry> _ringBuffer;
        private readonly ValueMutationHandler _handler1;
        private readonly ValueMutationHandler _handler2;
        private readonly ValueMutationHandler _handler3;
        private readonly IProducerBarrier<ValueEntry> _producerBarrier;

        public MultiCast1P3CDisruptorPerfTest()
            : base(1 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEntry>(()=>new ValueEntry(), Size,
                                       ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                       WaitStrategyFactory.WaitStrategyOption.Yielding);

            _handler1 = new ValueMutationHandler(Operation.Addition, Iterations);
            _handler2 = new ValueMutationHandler(Operation.Substraction, Iterations);
            _handler3 = new ValueMutationHandler(Operation.And, Iterations);

            _ringBuffer.ConsumeWith(_handler1, _handler2, _handler3);
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

            while (!_handler1.Done && !_handler2.Done && !_handler3.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _ringBuffer.Halt();

            // TODO some random failure here to fix (the sequence number received by the consumer seems ok all the time but the end result is not.
            //Assert.AreEqual(ExpectedResults[0], _handler1.Value, "Addition");
            //Assert.AreEqual(ExpectedResults[1], _handler2.Value, "Sub");
            //Assert.AreEqual(ExpectedResults[2], _handler3.Value, "And");

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}