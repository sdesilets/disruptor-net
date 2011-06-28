using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests
{
    /**
    * <pre>
    * UniCast a series of items between 1 producer and 1 consumer.
    *
    * +----+    +----+
    * | P0 |--->| C0 |
    * +----+    +----+
    *
    *
    * Queue Based:
    * ============
    *
    *        put      take
    * +----+    +====+    +----+
    * | P0 |--->| Q0 |<---| C0 |
    * +----+    +====+    +----+
    *
    * P0 - Producer 0
    * Q0 - Queue 0
    * C0 - Consumer 0
    *
    *
    * Disruptor:
    * ==========
    *                   track to prevent wrap
    *             +-----------------------------+
    *             |                             |
    *             |                             v
    * +----+    +====+    +====+    +====+    +----+
    * | P0 |--->| PB |--->| RB |<---| CB |    | C0 |
    * +----+    +====+    +====+    +====+    +----+
    *                claim      get    ^        |
    *                                  |        |
    *                                  +--------+
    *                                    waitFor
    *
    * P0 - Producer 0
    * PB - ProducerBarrier
    * RB - RingBuffer
    * CB - ConsumerBarrier
    * C0 - Consumer 0
    *
    * </pre>
    */
    [TestFixture]
    public class UniCast1P1CPerfTest : AbstractPerfTestQueueVsDisruptor
    {
        private const int Size = 1024 * 32;
        //private const long Iterations = 1000L * 1000L * 500L;
        private const long Iterations = 1000L * 1000L * 10L;

        private static long ExpectedResult
        {
            get
            {
                var temp = 0L;
                for (var i = 0L; i < Iterations; i++)
                {
                    temp += i;
                }

                return temp;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly BlockingCollection<long> _queue = new BlockingCollection<long>(Size);
        private readonly ValueAdditionQueueConsumer _queueConsumer;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly RingBuffer<ValueEntry> _ringBuffer;
        private readonly IConsumerBarrier<ValueEntry> _consumerBarrier;
        private readonly ValueAdditionHandler _handler;
        private readonly BatchConsumer<ValueEntry> _batchConsumer;
        private readonly IProducerBarrier<ValueEntry> _producerBarrier;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public UniCast1P1CPerfTest()
        {
            _queueConsumer = new ValueAdditionQueueConsumer(_queue);

            _ringBuffer = new RingBuffer<ValueEntry>(new ValueEntryFactory(), Size, 
                                                     ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                                     WaitStrategyFactory.WaitStrategyOption.Yielding);

            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _handler = new ValueAdditionHandler();
            _batchConsumer = new BatchConsumer<ValueEntry>(_consumerBarrier, _handler);
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumer);
        }

        [Test]
        public override void ShouldCompareDisruptorVsQueues()
        {
            TestImplementations();
        }

        protected override long RunQueuePass(int passNumber)
        {
            _queueConsumer.Reset();

            var cts = new CancellationTokenSource();
            Task.Factory.StartNew(_queueConsumer.Run, cts.Token);
            
            var sw = Stopwatch.StartNew();

            for (var i = 0; i < Iterations; i++)
            {
                _queue.Add(i);
            }

            const long expectedSequence = Iterations - 1L;
            while (_queueConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / (sw.ElapsedMilliseconds);
            _queueConsumer.Halt();

            cts.Cancel(true);

            Assert.AreEqual(ExpectedResult, _queueConsumer.Value);

            return opsPerSecond;
        }

        protected override long RunDisruptorPass(int passNumber)
        {
            _handler.Reset();

            Task.Factory.StartNew(_batchConsumer.Run);

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                var entry = _producerBarrier.NextEntry();
                entry.Value = i;
                _producerBarrier.Commit(entry);
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_batchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / (sw.ElapsedMilliseconds);
            _batchConsumer.Halt();

            Assert.AreEqual(ExpectedResult, _handler.Value);

            return opsPerSecond;
        }
    }
}
