/**
 * <pre>
 *
 * Sequence a series of events from multiple producers going to one consumer.
 *
 * +----+
 * | P0 |------+
 * +----+      |
 *             v
 * +----+    +----+
 * | P1 |--->| C1 |
 * +----+    +----+
 *             ^
 * +----+      |
 * | P2 |------+
 * +----+
 *
 *
 * Queue Based:
 * ============
 *
 * +----+  put
 * | P0 |------+
 * +----+      |
 *             v   take
 * +----+    +====+    +----+
 * | P1 |--->| Q0 |<---| C0 |
 * +----+    +====+    +----+
 *             ^
 * +----+      |
 * | P2 |------+
 * +----+
 *
 * P0 - Producer 0
 * P1 - Producer 1
 * P2 - Producer 2
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
 *             ^  claim      get    ^        |
 * +----+      |                    |        |
 * | P1 |------+                    +--------+
 * +----+      |                      waitFor
 *             |
 * +----+      |
 * | P2 |------+
 * +----+
 *
 * P0 - Producer 0
 * P1 - Producer 1
 * P2 - Producer 2
 * PB - ProducerBarrier
 * RB - RingBuffer
 * CB - ConsumerBarrier
 * C0 - Consumer 0
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
    public class Sequencer3P1CPerfTest : AbstractPerfTestQueueVsDisruptorVsTplDataflow
    {
        private const int NumProducers = 3;
        private const int Size = 1024 * 32;
        private const long Iterations = 1000L * 1000L * 10L;
        private Barrier _testStartBarrier = new Barrier(NumProducers + 1);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly BlockingCollection<long> _blockingQueue = new BlockingCollection<long>(Size);
        private readonly ValueAdditionQueueConsumer _queueConsumer;
        private ValueQueueProducer[] _valueQueueProducers;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly ValueTypeRingBuffer<long> _ringBuffer;
        private readonly IConsumerBarrier<long> _consumerBarrier;
        private readonly ValueAdditionHandler _handler = new ValueAdditionHandler();
        private readonly BatchConsumer<long> _batchConsumer;
        private readonly IValueTypeProducerBarrier<long> _producerBarrier;
        private ValueProducer[] _valueProducers;

        public Sequencer3P1CPerfTest()
        {
            _queueConsumer = new ValueAdditionQueueConsumer(_blockingQueue, Iterations);

            _ringBuffer = new ValueTypeRingBuffer<long>(Size, 
                                   ClaimStrategyFactory.ClaimStrategyOption.Multithreaded,
                                   WaitStrategyFactory.WaitStrategyOption.Yielding);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _batchConsumer = new BatchConsumer<long>(_consumerBarrier, _handler);
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumer);
        }

        protected override long RunQueuePass(int passNumber)
        {
            _queueConsumer.Reset();

            for (var i = 0; i < NumProducers; i++)
            {
                (new Thread(_valueQueueProducers[i].Run) { Name = "Queue producer " + i }).Start();
            }
            (new Thread(_queueConsumer.Run) { Name = "Queue consumer" }).Start();
            
            var sw = Stopwatch.StartNew();
            _testStartBarrier.SignalAndWait();
            
            while (!_queueConsumer.Done)
            {
                // busy spin
            }

            var opsPerSecond = (NumProducers * Iterations * 1000L) / sw.ElapsedMilliseconds;
            _batchConsumer.Halt();

            return opsPerSecond;
        }

        protected override long RunDisruptorPass(int passNumber)
        {
            for (var i = 0; i < NumProducers; i++)
            {
                (new Thread(_valueProducers[i].Run) { Name = "Value producer " + i }).Start();
            }
            (new Thread(_batchConsumer.Run) { Name = "Batch consumer" }).Start();

            var sw = Stopwatch.StartNew();
            _testStartBarrier.SignalAndWait(); // test starts when every thread has signaled the barrier

            var expectedSequence = (Iterations * NumProducers * (passNumber + 1L)) - 1L;
            while (expectedSequence > _batchConsumer.Sequence)
            {
                // busy spin
            }

            var opsPerSecond = (NumProducers * Iterations * 1000L) / sw.ElapsedMilliseconds;
            _batchConsumer.Halt();

            return opsPerSecond;
        }

        protected override long RunTplDataflowPass(int passNumber)
        {
            return 0L;
        }

        protected override void SetUp(int passNumber)
        {
            _testStartBarrier = new Barrier(NumProducers + 1);

            _valueQueueProducers = new[]
                                      {
                                          new ValueQueueProducer(_testStartBarrier, _blockingQueue, Iterations),
                                          new ValueQueueProducer(_testStartBarrier, _blockingQueue, Iterations),
                                          new ValueQueueProducer(_testStartBarrier, _blockingQueue, Iterations)
                                      };

            _valueProducers = new[]
                                 {
                                     new ValueProducer(_testStartBarrier, _producerBarrier, Iterations),
                                     new ValueProducer(_testStartBarrier, _producerBarrier, Iterations),
                                     new ValueProducer(_testStartBarrier, _producerBarrier, Iterations)
                                 };
        }

        [Test]
        public override void ShouldCompareDisruptorVsQueues()
        {
            TestImplementations();
        }
    }
}
