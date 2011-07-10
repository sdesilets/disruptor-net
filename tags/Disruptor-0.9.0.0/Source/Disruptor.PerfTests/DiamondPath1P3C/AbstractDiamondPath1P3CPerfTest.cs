using Disruptor.PerfTests.Runner;

namespace Disruptor.PerfTests.DiamondPath1P3C
{
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
    public abstract class AbstractDiamondPath1P3CPerfTest:ThroughputPerfTest
    {
        protected const int Size = 1024 * 32;
        private long _expectedResult;

        protected long ExpectedResult
        {
            get
            {
                if (_expectedResult == 0)
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
    }
}