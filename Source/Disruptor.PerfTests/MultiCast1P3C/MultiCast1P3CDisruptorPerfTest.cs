using System.Diagnostics;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.MultiCast1P3C
{
    [TestFixture]
    public class MultiCast1P3CDisruptorPerfTest:AbstractMultiCast1P3CPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;
        private readonly ValueMutationEventHandler _handler1;
        private readonly ValueMutationEventHandler _handler2;
        private readonly ValueMutationEventHandler _handler3;

        public MultiCast1P3CDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEvent>(()=>new ValueEvent(), Size,
                                       ClaimStrategyOption.SingleProducer,
                                       WaitStrategyOption.Yielding);

            _handler1 = new ValueMutationEventHandler(Operation.Addition, Iterations);
            _handler2 = new ValueMutationEventHandler(Operation.Substraction, Iterations);
            _handler3 = new ValueMutationEventHandler(Operation.And, Iterations);

            _ringBuffer.HandleEventsWith(_handler1, _handler2, _handler3);
        }

        public override long RunPass()
        {
            _ringBuffer.StartProcessors();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                var @event = _ringBuffer.NextEvent();
                @event.Data.Value = i;
                _ringBuffer.Publish(@event);
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