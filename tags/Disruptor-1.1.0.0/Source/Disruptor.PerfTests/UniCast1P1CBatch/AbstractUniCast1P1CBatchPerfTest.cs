using Disruptor.PerfTests.UniCast1P1C;

namespace Disruptor.PerfTests.UniCast1P1CBatch
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
    public abstract class AbstractUniCast1P1CBatchPerfTest : AbstractUniCast1P1CPerfTest
    {
        protected AbstractUniCast1P1CBatchPerfTest(int iterations) : base(iterations)
        {
        }
    }
}