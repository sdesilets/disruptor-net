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
        private long _expectedResult;
        protected const int Size = 1024 * 32;

        protected AbstractUniCast1P1CPerfTest(int iterations) : base(iterations)
        {
        }

        protected long ExpectedResult
        {
            get
            {
                if (_expectedResult == 0)
                {
                     for (var i = 0L; i < Iterations; i++)
                    {
                        _expectedResult += i;
                    }
                }
                return _expectedResult;
            }
        }

        public override int MinimumCoresRequired
        {
            get { return 2; }
        }
    }
}