/**
 * <pre>
 * Produce an event replicated to two consumer and fold back to a single third consumer.
 *
 *           +----+
 *    +----->| C0 |-----+
 *    |      +----+     |
 *    |                 v
 * +----+             +----+
 * | P0 |             | C2 |
 * +----+             +----+
 *    |                 ^
 *    |      +----+     |
 *    +----->| C1 |-----+
 *           +----+
 *
 *
 * Queue Based:
 * ============
 *                 take       put
 *     put   +====+    +----+    +====+  take
 *    +----->| Q0 |<---| C0 |--->| Q2 |<-----+
 *    |      +====+    +----+    +====+      |
 *    |                                      |
 * +----+    +====+    +----+    +====+    +----+
 * | P0 |--->| Q1 |<---| C1 |--->| Q3 |<---| C2 |
 * +----+    +====+    +----+    +====+    +----+
 *
 * P0 - Producer 0
 * Q0 - Queue 0
 * Q1 - Queue 1
 * Q2 - Queue 2
 * Q3 - Queue 3
 * C0 - Consumer 0
 * C1 - Consumer 1
 * C2 - Consumer 2
 *
 *
 * Disruptor:
 * ==========
 *                      track to prevent wrap
 *             +--------------------------------------+
 *             |                                      |
 *             |                                      v
 * +----+    +====+    +====+            +=====+    +----+
 * | P0 |--->| PB |--->| RB |<-----------| CB1 |<---| C2 |
 * +----+    +====+    +====+            +=====+    +----+
 *                claim   ^  get            |   waitFor
 *                        |                 |
 *                     +=====+    +----+    |
 *                     | CB0 |<---| C0 |<---+
 *                     +=====+    +----+    |
 *                        ^                 |
 *                        |       +----+    |
 *                        +-------| C1 |<---+
 *                      waitFor   +----+
 *
 * P0  - Producer 0
 * PB  - ProducerBarrier
 * RB  - RingBuffer
 * CB0 - ConsumerBarrier 0
 * C0  - Consumer 0
 * C1  - Consumer 1
 * CB1 - ConsumerBarrier 1
 * C2  - Consumer 2
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
    public class DiamondPath1P3CPerfTest : AbstractPerfTestQueueVsDisruptorVsTplDataflow
    {
        private const int Size = 1024 * 32;
        private const long Iterations = 1000 * 1000 * 10L;
        private long _expectedResult;

        public long ExpectedResult
        {
            get
            {
                if(_expectedResult == 0)
                {
                    for (long i = 0; i < Iterations; i++)
                    {
                        var fizz = 0 == (i % 3L);
                        var buzz = 0 == (i % 5L);

                        if (fizz && buzz)
                        {
                            ++_expectedResult;
                        }
                    }
                }
                return _expectedResult;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly BlockingCollection<long> _fizzInputQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _buzzInputQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<bool> _fizzOutputQueue = new BlockingCollection<bool>(Size);
        private readonly BlockingCollection<bool> _buzzOutputQueue = new BlockingCollection<bool>(Size);

        private readonly FizzBuzzQueueConsumer _fizzQueueConsumer;
        private readonly FizzBuzzQueueConsumer _buzzQueueConsumer;
        private readonly FizzBuzzQueueConsumer _fizzBuzzQueueConsumer;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly RingBuffer<FizzBuzzEntry> _ringBuffer;
        private readonly IConsumerBarrier<FizzBuzzEntry> _consumerBarrier;

        private readonly FizzBuzzHandler _fizzHandler;
        private readonly BatchConsumer<FizzBuzzEntry> _batchConsumerFizz;

        private readonly FizzBuzzHandler _buzzHandler;
        private readonly BatchConsumer<FizzBuzzEntry> _batchConsumerBuzz;

        private readonly IConsumerBarrier<FizzBuzzEntry> _consumerBarrierFizzBuzz;

        private readonly FizzBuzzHandler _fizzBuzzHandler;
        private readonly BatchConsumer<FizzBuzzEntry> _batchConsumerFizzBuzz;

        private readonly IReferenceTypeProducerBarrier<FizzBuzzEntry> _producerBarrier;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        
        public DiamondPath1P3CPerfTest()
        {
            _fizzQueueConsumer = new FizzBuzzQueueConsumer(FizzBuzzStep.Fizz, _fizzInputQueue, _buzzInputQueue, _fizzOutputQueue, _buzzOutputQueue, Iterations);
            _buzzQueueConsumer = new FizzBuzzQueueConsumer(FizzBuzzStep.Buzz, _fizzInputQueue, _buzzInputQueue, _fizzOutputQueue, _buzzOutputQueue, Iterations);
            _fizzBuzzQueueConsumer = new FizzBuzzQueueConsumer(FizzBuzzStep.FizzBuzz, _fizzInputQueue, _buzzInputQueue, _fizzOutputQueue, _buzzOutputQueue, Iterations);

            ///////////////////////////////////////////////////////////////////////////////////////////////

            _ringBuffer = new RingBuffer<FizzBuzzEntry>(() => new FizzBuzzEntry(), Size,
                                          ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                          WaitStrategyFactory.WaitStrategyOption.Yielding);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _fizzHandler = new FizzBuzzHandler(FizzBuzzStep.Fizz);
            _batchConsumerFizz = new BatchConsumer<FizzBuzzEntry>(_consumerBarrier, _fizzHandler);

            _buzzHandler = new FizzBuzzHandler(FizzBuzzStep.Buzz);
            _batchConsumerBuzz = new BatchConsumer<FizzBuzzEntry>(_consumerBarrier, _buzzHandler);
            _consumerBarrierFizzBuzz = _ringBuffer.CreateConsumerBarrier(_batchConsumerFizz, _batchConsumerBuzz);

            _fizzBuzzHandler = new FizzBuzzHandler(FizzBuzzStep.FizzBuzz);
            _batchConsumerFizzBuzz = new BatchConsumer<FizzBuzzEntry>(_consumerBarrierFizzBuzz, _fizzBuzzHandler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumerFizzBuzz);
        }

        protected override long RunQueuePass(int passNumber)
        {
            _fizzBuzzQueueConsumer.Reset();

            (new Thread(_fizzQueueConsumer.Run) { Name = "Fizz" }).Start();
            (new Thread(_buzzQueueConsumer.Run) { Name = "Buzz" }).Start();
            (new Thread(_fizzBuzzQueueConsumer.Run) { Name = "FizzBuzz" }).Start();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                
                _fizzInputQueue.Add(i);
                _buzzInputQueue.Add(i);
            }

            while (!_fizzBuzzQueueConsumer.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            Assert.AreEqual(ExpectedResult, _fizzBuzzQueueConsumer.FizzBuzzCounter);

            return opsPerSecond;
        }

        protected override long RunDisruptorPass(int passNumber)
        {
            _fizzBuzzHandler.Reset();

            (new Thread(_batchConsumerFizz.Run){Name = "Fizz"}).Start();
            (new Thread(_batchConsumerBuzz.Run){Name = "Buzz"}).Start();
            (new Thread(_batchConsumerFizzBuzz.Run){Name = "FizzBuzz"}).Start();

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                FizzBuzzEntry data;
                var sequence = _producerBarrier.NextEntry(out data);
                data.Value = i;
                _producerBarrier.Commit(sequence);
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_batchConsumerFizzBuzz.Sequence < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _batchConsumerFizz.Halt();
            _batchConsumerBuzz.Halt();
            _batchConsumerFizzBuzz.Halt();

            Assert.AreEqual(ExpectedResult, _fizzBuzzHandler.FizzBuzzCounter);

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
