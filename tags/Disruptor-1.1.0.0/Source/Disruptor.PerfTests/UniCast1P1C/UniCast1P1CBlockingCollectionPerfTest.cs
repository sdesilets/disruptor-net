using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

/*
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
*/

namespace Disruptor.PerfTests.UniCast1P1C
{
    [TestFixture]
    public class UniCast1P1CBlockingCollectionPerfTest : AbstractUniCast1P1CPerfTest
    {
        private readonly BlockingCollection<long> _queue = new BlockingCollection<long>(Size);
        private readonly ValueAdditionQueueConsumer _queueConsumer;

        public UniCast1P1CBlockingCollectionPerfTest() : base(2 * Million)
        {
            _queueConsumer = new ValueAdditionQueueConsumer(_queue, Iterations);
        }

        public override long RunPass()
        {
            _queueConsumer.Reset();

            var cts = new CancellationTokenSource();
            Task.Factory.StartNew(_queueConsumer.Run, cts.Token);

            var sw = Stopwatch.StartNew();

            for (var i = 0; i < Iterations; i++)
            {
                _queue.Add(i);
            }

            while (!_queueConsumer.Done)
            {
                // busy spin
            }

            var opsPerSecond = (Iterations * 1000L) / (sw.ElapsedMilliseconds);

            cts.Cancel(true);

            Assert.AreEqual(ExpectedResult, _queueConsumer.Value, "RunQueuePass");

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}