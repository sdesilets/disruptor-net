using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
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
     */
    [TestFixture]
    public class Pipeline3StepPerfTest : AbstractPerfTestQueueVsDisruptor
    {
        private const  int Size = 1024 * 32;
        private const long Iterations = 1000 * 1000 * 10L;
        private static long _expectedResult;
        private const long OperandTwoInitialValue = 777L;

        private static long ExpectedResult
        {
            get
            {
                if(_expectedResult == 0)
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private readonly BlockingCollection<long[]> _stepOneQueue = new BlockingCollection<long[]>(Size);
        private readonly BlockingCollection<long> _stepTwoQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepThreeQueue = new BlockingCollection<long>(Size);

        private readonly FunctionQueueConsumer _stepOneQueueConsumer;
        private readonly FunctionQueueConsumer _stepTwoQueueConsumer;
        private readonly FunctionQueueConsumer _stepThreeQueueConsumer;

        private readonly RingBuffer<FunctionEntry> _ringBuffer;

        private readonly IConsumerBarrier<FunctionEntry> _stepOneConsumerBarrier;
        private readonly BatchConsumer<FunctionEntry> _stepOneBatchConsumer;

        private readonly IConsumerBarrier<FunctionEntry> _stepTwoConsumerBarrier;
        private readonly BatchConsumer<FunctionEntry> _stepTwoBatchConsumer;

        private readonly IConsumerBarrier<FunctionEntry> _stepThreeConsumerBarrier;
        private readonly FunctionHandler _stepThreeFunctionHandler;
        private readonly BatchConsumer<FunctionEntry> _stepThreeBatchConsumer;

        private readonly IReferenceTypeProducerBarrier<FunctionEntry> _producerBarrier;
        

        ///////////////////////////////////////////////////////////////////////////////////////////////
    
        public Pipeline3StepPerfTest()
        {
            _stepOneQueueConsumer = new FunctionQueueConsumer(FunctionStep.One, _stepOneQueue, _stepTwoQueue, _stepThreeQueue, Iterations);
            _stepTwoQueueConsumer = new FunctionQueueConsumer(FunctionStep.Two, _stepOneQueue, _stepTwoQueue, _stepThreeQueue, Iterations);
            _stepThreeQueueConsumer = new FunctionQueueConsumer(FunctionStep.Three, _stepOneQueue, _stepTwoQueue, _stepThreeQueue, Iterations);

            _ringBuffer = new RingBuffer<FunctionEntry>(() => new FunctionEntry(), Size,
                                      ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded,
                                      WaitStrategyFactory.WaitStrategyOption.Yielding);

            _stepOneConsumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _stepOneBatchConsumer = new BatchConsumer<FunctionEntry>(_stepOneConsumerBarrier, new FunctionHandler(FunctionStep.One));

            _stepTwoConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepOneBatchConsumer);
            _stepTwoBatchConsumer = new BatchConsumer<FunctionEntry>(_stepTwoConsumerBarrier, new FunctionHandler(FunctionStep.Two));

            _stepThreeConsumerBarrier = _ringBuffer.CreateConsumerBarrier(_stepTwoBatchConsumer);
            _stepThreeFunctionHandler = new FunctionHandler(FunctionStep.Three);
            _stepThreeBatchConsumer = new BatchConsumer<FunctionEntry>(_stepThreeConsumerBarrier, _stepThreeFunctionHandler);

            _producerBarrier = _ringBuffer.CreateProducerBarrier(_stepThreeBatchConsumer);
        }

        protected override long RunQueuePass(int passNumber)
        {
            _stepThreeQueueConsumer.Reset();

            ThreadPool.QueueUserWorkItem(_ => _stepOneQueueConsumer.Run());
            ThreadPool.QueueUserWorkItem(_ => _stepTwoQueueConsumer.Run());
            ThreadPool.QueueUserWorkItem(_ => _stepThreeQueueConsumer.Run());

            var sw = Stopwatch.StartNew();

            var operandTwo = OperandTwoInitialValue;
            for (long i = 0; i < Iterations; i++)
            {
                var values = new long[2];
                values[0] = i;
                values[1] = operandTwo--;
                _stepOneQueue.Add(values);
            }

            while (!_stepThreeQueueConsumer.Done)
            {
                // busy spin
            }

            var opsPerSecond= (Iterations * 1000L) / sw.ElapsedMilliseconds;

            Assert.AreEqual(ExpectedResult, _stepThreeQueueConsumer.StepThreeCounter);

            return opsPerSecond;
        }

        protected override long RunDisruptorPass(int passNumber)
        {
            _stepThreeFunctionHandler.Reset();

            new Thread(_stepOneBatchConsumer.Run){Name="Step 1"}.Start();
            new Thread(_stepTwoBatchConsumer.Run) { Name = "Step 2" }.Start();
            new Thread(_stepThreeBatchConsumer.Run) { Name = "Step 3" }.Start();

            var sw = Stopwatch.StartNew();

            var operandTwo = OperandTwoInitialValue;
            for (long i = 0; i < Iterations; i++)
            {
                FunctionEntry data;
                var sequence = _producerBarrier.NextEntry(out data);
                data.OperandOne = i;
                data.OperandTwo = operandTwo--;
                _producerBarrier.Commit(sequence);
            }

            var expectedSequence = _ringBuffer.Cursor;
            while (_stepThreeBatchConsumer.Sequence < expectedSequence)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _stepOneBatchConsumer.Halt();
            _stepTwoBatchConsumer.Halt();
            _stepThreeBatchConsumer.Halt();

            Assert.AreEqual(ExpectedResult, _stepThreeFunctionHandler.StepThreeCounter);

            return opsPerSecond;
        }

        [Test]
        public override void ShouldCompareDisruptorVsQueues()
        {
            TestImplementations();
        }
    }
}
