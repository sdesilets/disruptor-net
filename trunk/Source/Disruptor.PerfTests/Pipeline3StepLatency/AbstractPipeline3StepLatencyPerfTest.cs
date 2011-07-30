using System;
using System.Diagnostics;
using Disruptor.Collections;

namespace Disruptor.PerfTests.Pipeline3StepLatency
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
    public abstract class AbstractPipeline3StepLatencyPerfTest :LatencyPerfTest
    {
        protected const int Size = 1024 * 32;
        protected const long PauseNanos = 1000;
        protected static readonly double TicksToNanos = 1000 * 1000 * 1000 / (double)Stopwatch.Frequency;
        private Histogram _histogram;
        private static long _stopwatchTimestampCostInNano;

        protected AbstractPipeline3StepLatencyPerfTest(int iterations) : base(iterations)
        {
        }

        public override Histogram Histogram
        {
            get
            {
                if (_histogram == null)
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

        protected static long StopwatchTimestampCostInNano
        {
            get
            {
                if (_stopwatchTimestampCostInNano == 0)
                {
                    const long iterations = 10 * 1000 * 1000;
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
                    _stopwatchTimestampCostInNano = (long)(((finish - start) / (double)iterations) * TicksToNanos);
                }
                return _stopwatchTimestampCostInNano;
            }
        }

        public override int MinimumCoresRequired
        {
            get { return 4; }
        }
    }
}