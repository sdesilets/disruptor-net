namespace Disruptor.PerfTests.Pipeline3Step
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
     */
    public abstract class AbstractPipeline3StepPerfTest:ThroughputPerfTest
    {
        protected const int Size = 1024 * 32;
        private static long _expectedResult;
        protected const long OperandTwoInitialValue = 777L;

        protected long ExpectedResult
        {
            get
            {
                if (_expectedResult == 0)
                {
                    var operandTwo = OperandTwoInitialValue;

                    for (long i = 0; i < Iterations; i++)
                    {
                        var stepOneResult = i + operandTwo--;
                        var stepTwoResult = stepOneResult + 3;

                        if ((stepTwoResult & 4L) == 4L)
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