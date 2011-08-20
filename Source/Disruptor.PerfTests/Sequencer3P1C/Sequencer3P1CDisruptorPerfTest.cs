using System.Diagnostics;
using System.Threading;
using Disruptor.PerfTests.Support;
using NUnit.Framework;

namespace Disruptor.PerfTests.Sequencer3P1C
{
    [TestFixture]
    public class Sequencer3P1CDisruptorPerfTest : AbstractSequencer3P1CPerfTest
    {
        private readonly RingBuffer<ValueEvent> _ringBuffer;
        private readonly ValueAdditionEventHandler _eventHandler;
        private readonly ValueProducer[] _valueProducers;
        private readonly Barrier _testStartBarrier = new Barrier(NumProducers);

        public Sequencer3P1CDisruptorPerfTest()
            : base(20 * Million)
        {
            _ringBuffer = new RingBuffer<ValueEvent>(()=>new ValueEvent(), Size,
                                   ClaimStrategyOption.MultipleProducers,
                                   WaitStrategyOption.Yielding);

            _eventHandler = new ValueAdditionEventHandler(Iterations * NumProducers);
            _ringBuffer.HandleEventsWith(_eventHandler);
            
            _valueProducers = new ValueProducer[NumProducers];

            for (int i = 0; i < NumProducers; i++)
            {
                _valueProducers[i] = new ValueProducer(_testStartBarrier, _ringBuffer, Iterations);
            }
        }

        public override long RunPass()
        {
            _ringBuffer.StartProcessors();

            for (var i = 0; i < NumProducers - 1; i++)
            {
                (new Thread(_valueProducers[i].Run) { Name = "Value producer " + i }).Start();
            }
            
            var sw = Stopwatch.StartNew();
            _valueProducers[NumProducers - 1].Run();

            while (!_eventHandler.Done)
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