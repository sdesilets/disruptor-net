
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Disruptor.Collections;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests
{
    /**
     * <pre>
     *
     * Pipeline a series of stages from a producer to ultimate consumer.
     * Each consumer depends on the output of the previous consumer.
     *
     * +----+    +----+    +----+    +----+
     * | P0 |--->| C0 |--->| C1 |--->| C2 |
     * +----+    +----+    +----+    +----+
     *
     *
     * Queue Based:
     * ============
     *
     *        put      take       put      take       put      take
     * +----+    +====+    +----+    +====+    +----+    +====+    +----+
     * | P0 |--->| Q0 |<---| C0 |--->| Q1 |<---| C1 |--->| Q2 |<---| C2 |
     * +----+    +====+    +----+    +====+    +----+    +====+    +----+
     *
     * P0 - Producer 0
     * Q0 - Queue 0
     * C0 - Consumer 0
     * Q1 - Queue 1
     * C1 - Consumer 1
     * Q2 - Queue 2
     * C2 - Consumer 1
     *
     *
     * Disruptor:
     * ==========
     *                   track to prevent wrap
     *             +------------------------------------------------------------------------+
     *             |                                                                        |
     *             |                                                                        v
     * +----+    +====+    +====+    +=====+    +----+    +=====+    +----+    +=====+    +----+
     * | P0 |--->| PB |--->| RB |    | CB0 |<---| C0 |<---| CB1 |<---| C1 |<---| CB2 |<---| C2 |
     * +----+    +====+    +====+    +=====+    +----+    +=====+    +----+    +=====+    +----+
     *                claim   ^  get    |  waitFor           |  waitFor           |  waitFor
     *                        |         |                    |                    |
     *                        +---------+--------------------+--------------------+
     *
     *
     * P0  - Producer 0
     * PB  - ProducerBarrier
     * RB  - RingBuffer
     * CB0 - ConsumerBarrier 0
     * C0  - Consumer 0
     * CB1 - ConsumerBarrier 1
     * C1  - Consumer 1
     * CB2 - ConsumerBarrier 2
     * C2  - Consumer 2
     *
     * </pre>
     *
     * Note: <b>This test is only useful on a system using an invariant TSC in user space from the System.nanoTime call.</b>
     */
    [TestFixture]
    public class Pipeline3StepLatencyPerfTest
    {
        private const int Size = 1024 * 32;
        private const long Iterations = 1000 * 1000 * 10L;//1000 * 1000 * 50;
        private const long PauseNanos = 1000;
        private readonly double _ticksToNanos;
        private Histogram _histogram;
        private long _stopwatchTimestampCostInNano;

        private Histogram Histogram
        {
            get
            {
                if(_histogram == null)
                {
                    var intervals = new long[31];
                    var intervalUpperBound = 1L;
                    for (var i = 0; i < intervals.Length - 1; i++)
                    {
                        intervalUpperBound *= 2;
                        intervals[i] = intervalUpperBound;
                    }

                    intervals[intervals.Length - 1] = long.MaxValue;
                    _histogram = new Histogram(intervals);
                }
                return _histogram;
            }
        }

        private long StopwatchTimestampCostInNano
        {
            get
            {
                if(_stopwatchTimestampCostInNano == 0)
                {
                    const long iterations = 10*1000*1000;
                    var start = Stopwatch.GetTimestamp();
                    var finish = start;

                    for (var i = 0; i < iterations; i++)
                    {
                        finish = Stopwatch.GetTimestamp();
                    }

                    if (finish <= start)
                    {
                        throw new Exception();
                    }

                    finish = Stopwatch.GetTimestamp();
                    _stopwatchTimestampCostInNano = (long)( ((finish - start) / (double)iterations) * _ticksToNanos);
                }
                return _stopwatchTimestampCostInNano;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly BlockingCollection<long> _stepOneQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepTwoQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepThreeQueue = new BlockingCollection<long>(Size);

        private LatencyStepQueueConsumer _stepOneQueueConsumer;
        private LatencyStepQueueConsumer _stepTwoQueueConsumer;
        private LatencyStepQueueConsumer _stepThreeQueueConsumer;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ValueTypeRingBuffer<long> _ringBuffer;

        private IConsumerBarrier<long> _stepOneConsumerBarrier;
        private LatencyStepHandler _stepOneFunctionHandler;
        private BatchConsumer<long> _stepOneBatchConsumer;

        private IConsumerBarrier<long> _stepTwoConsumerBarrier;
        private LatencyStepHandler _stepTwoFunctionHandler;
        private BatchConsumer<long> _stepTwoBatchConsumer;

        private IConsumerBarrier<long> _stepThreeConsumerBarrier;
        private LatencyStepHandler _stepThreeFunctionHandler;
        private BatchConsumer<long> _stepThreeBatchConsumer;

        private IValueTypeProducerBarrier<long> _producerBarrier;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Pipeline3StepLatencyPerfTest()
        {
            _ticksToNanos = 1000 * 1000 * 1000 / (double)Stopwatch.Frequency;
        }

        [Test]
        public void ShouldCompareDisruptorVsQueues()
        {
            const int runs = 3;

            Assert.IsTrue(Stopwatch.IsHighResolution, "The test requires high resolution");

            for (var i = 0; i < runs; i++)
            {
                GC.Collect();

                SetUp();

                Histogram.Clear();
                RunDisruptorPass();
                Assert.AreEqual(Iterations, Histogram.Count);
                Console.WriteLine("{0} run {1} Disruptor {2}\n", GetType().Name, i, Histogram);
                var disruptorMeanLatency = Histogram.Mean;
                DumpHistogram();

                Histogram.Clear();
                RunQueuePass();
                Assert.AreEqual(Iterations, Histogram.Count);
                var queueMeanLatency = Histogram.Mean;
                Console.WriteLine("{0} run {1} Queues {2}\n", GetType().Name, i, Histogram);
                DumpHistogram();

                Assert.IsTrue(queueMeanLatency > disruptorMeanLatency);
            }
        }

        private void SetUp()
        {
            _stepOneQueueConsumer = new LatencyStepQueueConsumer(FunctionStep.One, _stepOneQueue, _stepTwoQueue, Histogram, StopwatchTimestampCostInNano, _ticksToNanos, Iterations);
            _stepTwoQueueConsumer = new LatencyStepQueueConsumer(FunctionStep.Two, _stepTwoQueue, _stepThreeQueue, Histogram, StopwatchTimestampCostInNano, _ticksToNanos, Iterations);
            _stepThreeQueueConsumer = new LatencyStepQueueConsumer(FunctionStep.Three, _stepThreeQueue, null, Histogram, StopwatchTimestampCostInNano, _ticksToNanos, Iterations);


            /////////////////////////////////////////////
            _ringBuffer = new ValueTypeRingBuffer<long>(Size,
                                   ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                   WaitStrategyFactory.WaitStrategyOption.BusySpin);
            _stepOneConsumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _stepOneFunctionHandler = new LatencyStepHandler(FunctionStep.One, Histogram, StopwatchTimestampCostInNano, _ticksToNanos);
            _stepOneBatchConsumer = new BatchConsumer<long>(_stepOneConsumerBarrier, _stepOneFunctionHandler);

            _stepTwoConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepOneBatchConsumer);
            _stepTwoFunctionHandler = new LatencyStepHandler(FunctionStep.Two, Histogram, StopwatchTimestampCostInNano, _ticksToNanos);
            _stepTwoBatchConsumer = new BatchConsumer<long>(_stepTwoConsumerBarrier, _stepTwoFunctionHandler);

            _stepThreeConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepTwoBatchConsumer);
            _stepThreeFunctionHandler = new LatencyStepHandler(FunctionStep.Three, Histogram, StopwatchTimestampCostInNano, _ticksToNanos);
            _stepThreeBatchConsumer = new BatchConsumer<long>(_stepThreeConsumerBarrier, _stepThreeFunctionHandler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier(_stepThreeBatchConsumer);
        }

        private void DumpHistogram()
        {
            for (var i = 0; i < Histogram.Size; i++)
            {
                Console.Write(Histogram.GetUpperBoundAt(i));
                Console.Write("\t");
                Console.Write(Histogram.GetCountAt(i));
                Console.WriteLine();
            }
        }

        private void RunDisruptorPass()
        {
            new Thread(_stepOneBatchConsumer.Run){Name = "Step 1 disruptor"}.Start();
            new Thread(_stepTwoBatchConsumer.Run) { Name = "Step 2 disruptor" }.Start();
            new Thread(_stepThreeBatchConsumer.Run) { Name = "Step 3 disruptor" }.Start();
        
            for (long i = 0; i < Iterations; i++)
            {
                _producerBarrier.Commit(Stopwatch.GetTimestamp());

                var pauseStart = Stopwatch.GetTimestamp();
                while (PauseNanos > (Stopwatch.GetTimestamp() -  pauseStart) * _ticksToNanos)
                {
                    // busy spin
                }
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_stepThreeBatchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            _stepOneBatchConsumer.Halt();
            _stepTwoBatchConsumer.Halt();
            _stepThreeBatchConsumer.Halt();
        }

        private void RunQueuePass()
        {
            _stepThreeQueueConsumer.Reset();

            new Thread(_stepOneQueueConsumer.Run) { Name = "Step 1 queues" }.Start();
            new Thread(_stepTwoQueueConsumer.Run) { Name = "Step 2 queues" }.Start();
            new Thread(_stepThreeQueueConsumer.Run) { Name = "Step 3 queues" }.Start();

            for (long i = 0; i < Iterations; i++)
            {
                _stepOneQueue.Add(Stopwatch.GetTimestamp());

                var pauseStart = Stopwatch.GetTimestamp();
                while (PauseNanos > (Stopwatch.GetTimestamp() - pauseStart) * _ticksToNanos)
                {
                    // busy spin
                }
            }

            while (!_stepThreeQueueConsumer.Done)
            {
                // busy spin
            }
        }
    }
}
