using Disruptor.PerfTests.Runner;
using Disruptor.PerfTests.Support;

namespace Disruptor.PerfTests.MultiCast1P3C
{
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
    public abstract class AbstractMultiCast1P3CPerfTest:ThroughputPerfTest
    {
        protected const int NumConsumers = 3;
        protected const int Size = 1024 * 32;
        private long[] _results;

        protected long[] ExpectedResults
        {
            get
            {
                if (_results == null)
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

        
    }
}