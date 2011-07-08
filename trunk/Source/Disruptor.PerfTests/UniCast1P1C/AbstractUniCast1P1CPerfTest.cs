namespace Disruptor.PerfTests.UniCast1P1C
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
    public abstract class AbstractUniCast1P1CPerfTest : ThroughputPerfTest
    {
        protected long ExpectedResult
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
    }
}