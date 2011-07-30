using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.MultiCast1P3C
{
    [TestFixture]
    public class MultiCast1P3CBlockingCollectionPerfTest : AbstractMultiCast1P3CPerfTest
    {
        private readonly BlockingCollection<long>[] _blockingQueues = new[]
                                                                {
                                                                    new BlockingCollection<long>(Size),
                                                                    new BlockingCollection<long>(Size),
                                                                    new BlockingCollection<long>(Size)
                                                                };

        private readonly ValueMutationQueueConsumer[] _queueConsumers = new ValueMutationQueueConsumer[NumConsumers];

        public MultiCast1P3CBlockingCollectionPerfTest()
            : base(1 * Million)
        {
            _queueConsumers[0] = new ValueMutationQueueConsumer(_blockingQueues[0], Operation.Addition, Iterations);
            _queueConsumers[1] = new ValueMutationQueueConsumer(_blockingQueues[1], Operation.Substraction, Iterations);
            _queueConsumers[2] = new ValueMutationQueueConsumer(_blockingQueues[2], Operation.And, Iterations);
        }

        public override long RunPass()
        {
            for (var i = 0; i < NumConsumers; i++)
            {
                _queueConsumers[i].Reset();
                (new Thread(_queueConsumers[i].Run) { Name = string.Format("Queue consumer {0}", i) }).Start();
            }

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                _blockingQueues[0].Add(i);
                _blockingQueues[1].Add(i);
                _blockingQueues[2].Add(i);
            }

            while (!AllConsumersAreDone())
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;
            for (var i = 0; i < NumConsumers; i++)
            {
                Assert.AreEqual(ExpectedResults[i], _queueConsumers[i].Value);
            }

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }


        private bool AllConsumersAreDone()
        {
            return _queueConsumers[0].Done && _queueConsumers[1].Done && _queueConsumers[2].Done;
        }
    }
}