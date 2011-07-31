using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3Step
{
    [TestFixture]
    public class Pipeline3StepBlockingCollectionPerfTest:AbstractPipeline3StepPerfTest
    {
        private readonly BlockingCollection<long[]> _stepOneQueue = new BlockingCollection<long[]>(Size);
        private readonly BlockingCollection<long> _stepTwoQueue = new BlockingCollection<long>(Size);
        private readonly BlockingCollection<long> _stepThreeQueue = new BlockingCollection<long>(Size);

        private readonly FunctionQueueConsumer _stepOneQueueConsumer;
        private readonly FunctionQueueConsumer _stepTwoQueueConsumer;
        private readonly FunctionQueueConsumer _stepThreeQueueConsumer;

        public Pipeline3StepBlockingCollectionPerfTest() : base(1*Million)
        {
            _stepOneQueueConsumer = new FunctionQueueConsumer(FunctionStep.One, _stepOneQueue, _stepTwoQueue, _stepThreeQueue, Iterations);
            _stepTwoQueueConsumer = new FunctionQueueConsumer(FunctionStep.Two, _stepOneQueue, _stepTwoQueue, _stepThreeQueue, Iterations);
            _stepThreeQueueConsumer = new FunctionQueueConsumer(FunctionStep.Three, _stepOneQueue, _stepTwoQueue, _stepThreeQueue, Iterations);
        }

        public override long RunPass()
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

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            Assert.AreEqual(ExpectedResult, _stepThreeQueueConsumer.StepThreeCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}