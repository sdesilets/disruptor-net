using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Sequencer3P1C
{
    [TestFixture]
    public class Sequencer3P1CDisruptorPerfTest : AbstractSequencer3P1CPerfTest
    {
        private readonly RingBuffer<ValueEntry> _ringBuffer;
        private readonly ValueAdditionHandler _handler;
        private readonly IProducerBarrier<ValueEntry> _producerBarrier;
        private readonly ValueProducer[] _valueProducers;
        private readonly Barrier _testStartBarrier = new Barrier(NumProducers);

        public Sequencer3P1CDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEntry>(()=>new ValueEntry(), Size,
                                   ClaimStrategyFactory.ClaimStrategyOption.Multithreaded,
                                   WaitStrategyFactory.WaitStrategyOption.Yielding);

            _handler = new ValueAdditionHandler(Iterations * NumProducers);
            _ringBuffer.ConsumeWith(_handler);
            _producerBarrier = _ringBuffer.CreateProducerBarrier();
            
            _valueProducers = new ValueProducer[NumProducers];

            for (int i = 0; i < NumProducers; i++)
            {
                _valueProducers[i] = new ValueProducer(_testStartBarrier, _producerBarrier, Iterations);
            }
        }

        public override long RunPass()
        {
            _ringBuffer.StartConsumers();

            for (var i = 0; i < NumProducers - 1; i++)
            {
                (new Thread(_valueProducers[i].Run) { Name = "Value producer " + i }).Start();
            }
            
            var sw = Stopwatch.StartNew();
            _valueProducers[NumProducers - 1].Run();

            while (!_handler.Done)
            {
                // busy spin
            }

            var opsPerSecond = (NumProducers * Iterations * 1000L) / sw.ElapsedMilliseconds;
            _ringBuffer.Halt();

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}