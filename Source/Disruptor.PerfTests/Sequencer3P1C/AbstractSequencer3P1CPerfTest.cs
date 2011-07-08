using Disruptor.PerfTests.Runner;

namespace Disruptor.PerfTests.Sequencer3P1C
{

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

    public abstract class AbstractSequencer3P1CPerfTest:ThroughputPerfTest
    {
        protected const int NumProducers = 3;
        protected const int Size = 1024 * 32;
    }
}