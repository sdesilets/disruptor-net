/**
 * <pre>
 *
 * MultiCast a series of items between 1 producer and 3 consumers.
 *
 *           +----+
 *    +----->| C0 |
 *    |      +----+
 *    |
 * +----+    +----+
 * | P0 |--->| C1 |
 * +----+    +----+
 *    |
 *    |      +----+
 *    +----->| C2 |
 *           +----+
 *
 *
 * Queue Based:
 * ============
 *                 take
 *   put     +====+    +----+
 *    +----->| Q0 |<---| C0 |
 *    |      +====+    +----+
 *    |
 * +----+    +====+    +----+
 * | P0 |--->| Q1 |<---| C1 |
 * +----+    +====+    +----+
 *    |
 *    |      +====+    +----+
 *    +----->| Q2 |<---| C2 |
 *           +====+    +----+
 *
 * P0 - Producer 0
 * Q0 - Queue 0
 * Q1 - Queue 1
 * Q2 - Queue 2
 * C0 - Consumer 0
 * C1 - Consumer 1
 * C2 - Consumer 2
 *
 *
 * Disruptor:
 * ==========
 *                            track to prevent wrap
 *             +-----------------------------+---------+---------+
 *             |                             |         |         |
 *             |                             v         v         v
 * +----+    +====+    +====+    +====+    +----+    +----+    +----+
 * | P0 |--->| PB |--->| RB |<---| CB |    | C0 |    | C1 |    | C2 |
 * +----+    +====+    +====+    +====+    +----+    +----+    +----+
 *                claim      get    ^        |         |         |
 *                                  |        |         |         |
 *                                  +--------+---------+---------+
 *                                               waitFor
 *
 * P0 - Producer 0
 * PB - ProducerBarrier
 * RB - RingBuffer
 * CB - ConsumerBarrier
 * C0 - Consumer 0
 * C1 - Consumer 1
 * C2 - Consumer 2
 *
 * </pre>
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests
{
    [TestFixture]
    public class MultiCast1P3CPerfTest : AbstractPerfTestQueueVsDisruptorVsTplDataflow
    {
        private const int NumConsumers = 3;
        private const int Size = 1024 * 32;
        private const long Iterations = 1000 * 1000 * 10L;
        private long[] _results;

        private long[] ExpectedResults
        {
            get
            {
                if(_results == null)
                {
                    _results = new long[NumConsumers];
                    for (long i = 0; i < Iterations; i++)
                    {
                        _results[0] = Operation.Addition.Op(_results[0], i);
                        _results[1] = Operation.Substraction.Op(_results[1], i);
                        _results[2] = Operation.And.Op(_results[2], i);
                    }
                }
                return _results;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        private readonly BlockingCollection<long>[] _blockingQueues = new[]
                                                                {
                                                                    new BlockingCollection<long>(Size),
                                                                    new BlockingCollection<long>(Size),
                                                                    new BlockingCollection<long>(Size)
                                                                };

        private readonly ValueMutationQueueConsumer[] _queueConsumers = new ValueMutationQueueConsumer[NumConsumers];


        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly ValueTypeRingBuffer<long> _ringBuffer;

        private readonly IConsumerBarrier<long> _consumerBarrier;

        private readonly ValueMutationHandler[] _handlers = new []
                                                               {
                                                                   new ValueMutationHandler(Operation.Addition),
                                                                   new ValueMutationHandler(Operation.Substraction),
                                                                   new ValueMutationHandler(Operation.And),
                                                               };

        private readonly BatchConsumer<long>[] _batchConsumers;

        private readonly IValueTypeProducerBarrier<long> _producerBarrier;

        public MultiCast1P3CPerfTest()
        {
            _queueConsumers[0] = new ValueMutationQueueConsumer(_blockingQueues[0], Operation.Addition, Iterations);
            _queueConsumers[1] = new ValueMutationQueueConsumer(_blockingQueues[1], Operation.Substraction, Iterations);
            _queueConsumers[2] = new ValueMutationQueueConsumer(_blockingQueues[2], Operation.And, Iterations);

            ///////////////////////////////////////////////////////////////////////////////////////////////

            _ringBuffer = new ValueTypeRingBuffer<long>(Size,
                                       ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                       WaitStrategyFactory.WaitStrategyOption.Yielding);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();

            _batchConsumers = new []
                                 {
                                     new BatchConsumer<long>(_consumerBarrier, _handlers[0]),
                                     new BatchConsumer<long>(_consumerBarrier, _handlers[1]),
                                     new BatchConsumer<long>(_consumerBarrier, _handlers[2])
                                 };
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumers);
        }

        protected override long RunQueuePass(int passNumber)
        {
            for (var i = 0; i < NumConsumers; i++)
            {
                _queueConsumers[i].Reset();
                (new Thread(_queueConsumers[i].Run){Name = string.Format("Queue consumer {0}", i)}).Start();
            }

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                _blockingQueues[0].Add(i);
                _blockingQueues[1].Add(i);
                _blockingQueues[2].Add(i);
            }

            while (!AllConsumersAreDone())
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            for (var i = 0; i < NumConsumers; i++)
            {
                Assert.AreEqual(ExpectedResults[i], _queueConsumers[i].Value);
            }

            return opsPerSecond;
        }

        private bool AllConsumersAreDone()
        {
            return _queueConsumers[0].Done && _queueConsumers[1].Done && _queueConsumers[2].Done;
        }

        protected override long RunDisruptorPass(int passNumber)
        {
            for (var i = 0; i < NumConsumers; i++)
            {
                _handlers[i].Reset();
                (new Thread(_batchConsumers[i].Run){Name = string.Format("Batch consumer {0}", i)}).Start();
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

        protected override long RunTplDataflowPass(int passNumber)
        {
            return 0;
        }

        [Test]
        public override void ShouldCompareDisruptorVsQueues()
        {
            TestImplementations();
        }
    }
}
