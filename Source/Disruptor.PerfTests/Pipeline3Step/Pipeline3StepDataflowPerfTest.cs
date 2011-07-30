using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3Step
{
    [TestFixture]
    public class Pipeline3StepDataflowPerfTest:AbstractPipeline3StepPerfTest
    {
        private readonly BufferBlock<long[]> _stepOneTpl = new BufferBlock<long[]>();
        private readonly BufferBlock<long> _stepTwoTpl = new BufferBlock<long>();
        private readonly BufferBlock<long> _stepThreeTpl = new BufferBlock<long>();
        private readonly ActionBlock<long[]> _stepOneAb;
        private readonly ActionBlock<long> _stepTwoAb;

        public Pipeline3StepDataflowPerfTest()
            : base(1 * Million)
        {
            _stepOneAb = new ActionBlock<long[]>(values => _stepTwoTpl.Post(values[0] + values[1]));
            _stepTwoAb = new ActionBlock<long>(value => _stepThreeTpl.Post(value + 3));

            _stepOneTpl.LinkTo(_stepOneAb);
            _stepTwoTpl.LinkTo(_stepTwoAb);
        }

        public override long RunPass()
        {
            long tplResult = 0;
            var c = Task.Factory.StartNew(
                () =>
                    {
                        tplResult = 0L;
                        for (long i = 0; i < Iterations; i++)
                        {
                            long value = _stepThreeTpl.Receive();
                            var testValue = value;
                            if ((testValue & 4L) == 4L)
                            {
                                ++tplResult;
                            }
                        }
                    });

            var sw = Stopwatch.StartNew();

            var operandTwo = OperandTwoInitialValue;
            for (long i = 0; i < Iterations; i++)
            {
                var values = new long[2];
                values[0] = i;
                values[1] = operandTwo--;
                _stepOneTpl.Post(values);
            }

            Task.WaitAll(c);


            var opsPerSecond = (Iterations * 1000L) / (sw.ElapsedMilliseconds);

            Assert.AreEqual(ExpectedResult, tplResult);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}