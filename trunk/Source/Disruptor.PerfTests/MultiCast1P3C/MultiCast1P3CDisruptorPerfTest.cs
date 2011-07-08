using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.MultiCast1P3C
{
    [TestFixture]
    public class MultiCast1P3CDisruptorPerfTest:AbstractMultiCast1P3CPerfTest
    {
        private readonly ValueTypeRingBuffer<long> _ringBuffer;
        private readonly IConsumerBarrier<long> _consumerBarrier;
        private readonly ValueMutationHandler[] _handlers = new[]
                                                               {
                                                                   new ValueMutationHandler(Operation.Addition),
                                                                   new ValueMutationHandler(Operation.Substraction),
                                                                   new ValueMutationHandler(Operation.And),
                                                               };

        private readonly BatchConsumer<long>[] _batchConsumers;
        private readonly IValueTypeProducerBarrier<long> _producerBarrier;

        public MultiCast1P3CDisruptorPerfTest()
        {
            _ringBuffer = new ValueTypeRingBuffer<long>(Size,
                                       ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                       WaitStrategyFactory.WaitStrategyOption.Yielding);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();

            _batchConsumers = new[]
                                 {
                                     new BatchConsumer<long>(_consumerBarrier, _handlers[0]),
                                     new BatchConsumer<long>(_consumerBarrier, _handlers[1]),
                                     new BatchConsumer<long>(_consumerBarrier, _handlers[2])
                                 };
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumers);
        }

        public override long RunPass()
        {
            for (var i = 0; i < NumConsumers; i++)
            {
                _handlers[i].Reset();
                (new Thread(_batchConsumers[i].Run) { Name = string.Format("Batch consumer {0}", i) }).Start();
            }

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                _producerBarrier.Commit(i);
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_batchConsumers.GetMinimumSequence() < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            for (var i = 0; i < NumConsumers; i++)
            {
                _batchConsumers[i].Halt();
                Assert.AreEqual(ExpectedResults[i], _handlers[i].Value);
            }

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}