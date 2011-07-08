using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Sequencer3P1C
{
    [TestFixture]
    public class Sequencer3P1CDisruptorPerfTest : AbstractSequencer3P1CPerfTest
    {
        private readonly ValueTypeRingBuffer<long> _ringBuffer;
        private readonly IConsumerBarrier<long> _consumerBarrier;
        private readonly ValueAdditionHandler _handler = new ValueAdditionHandler();
        private readonly BatchConsumer<long> _batchConsumer;
        private readonly IValueTypeProducerBarrier<long> _producerBarrier;
        private readonly ValueProducer[] _valueProducers;
        private readonly Barrier _testStartBarrier = new Barrier(NumProducers + 1);

        public Sequencer3P1CDisruptorPerfTest()
        {
            _ringBuffer = new ValueTypeRingBuffer<long>(Size,
                                   ClaimStrategyFactory.ClaimStrategyOption.Multithreaded,
                                   WaitStrategyFactory.WaitStrategyOption.Yielding);
            _consumerBarrier = _ringBuffer.CreateConsumerBarrier();
            _batchConsumer = new BatchConsumer<long>(_consumerBarrier, _handler);
            _producerBarrier = _ringBuffer.CreateProducerBarrier(_batchConsumer);
            
            _valueProducers = new[]
                                 {
                                     new ValueProducer(_testStartBarrier, _producerBarrier, Iterations),
                                     new ValueProducer(_testStartBarrier, _producerBarrier, Iterations),
                                     new ValueProducer(_testStartBarrier, _producerBarrier, Iterations)
                                 };
        }

        public override long RunPass()
        {
            for (var i = 0; i < NumProducers; i++)
            {
                (new Thread(_valueProducers[i].Run) { Name = "Value producer " + i }).Start();
            }
            (new Thread(_batchConsumer.Run) { Name = "Batch consumer" }).Start();

            var sw = Stopwatch.StartNew();
            _testStartBarrier.SignalAndWait(); // test starts when every thread has signaled the barrier

            var expectedSequence = (Iterations * NumProducers) - 1L;
            while (expectedSequence > _batchConsumer.Sequence)
            {
                // busy spin
            }

            var opsPerSecond = (NumProducers * Iterations * 1000L) / sw.ElapsedMilliseconds;
            _batchConsumer.Halt();

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}