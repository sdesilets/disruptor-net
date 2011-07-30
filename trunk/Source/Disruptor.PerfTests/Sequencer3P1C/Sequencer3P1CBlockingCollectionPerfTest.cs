using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Sequencer3P1C
{
    [TestFixture]
    public class Sequencer3P1CBlockingCollectionPerfTest : AbstractSequencer3P1CPerfTest
    {
        private readonly BlockingCollection<long> _blockingQueue = new BlockingCollection<long>(Size);
        private readonly ValueAdditionQueueConsumer _queueConsumer;
        private readonly ValueQueueProducer[] _valueQueueProducers;
        private readonly Barrier _testStartBarrier = new Barrier(NumProducers + 1);

        public Sequencer3P1CBlockingCollectionPerfTest()
            : base(1 * Million)
        {
            _queueConsumer = new ValueAdditionQueueConsumer(_blockingQueue, Iterations);
            _testStartBarrier = new Barrier(NumProducers + 1);
            _valueQueueProducers = new ValueQueueProducer[NumProducers];

            for (int i = 0; i < NumProducers; i++)
            {
                _valueQueueProducers[i] = new ValueQueueProducer(_testStartBarrier, _blockingQueue, Iterations);
            }
        }

        public override long RunPass()
        {
            _queueConsumer.Reset();

            for (var i = 0; i < NumProducers; i++)
            {
                (new Thread(_valueQueueProducers[i].Run) { Name = "Queue producer " + i }).Start();
            }
            (new Thread(_queueConsumer.Run) { Name = "Queue consumer" }).Start();

            var sw = Stopwatch.StartNew();
            _testStartBarrier.SignalAndWait();

            while (!_queueConsumer.Done)
            {
                // busy spin
            }

            var opsPerSecond = (NumProducers * Iterations * 1000L) / sw.ElapsedMilliseconds;

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}