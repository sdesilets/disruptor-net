using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;

namespace Disruptor.PerfTests.UniCast1P1C
{
    [TestFixture]
    public class UniCast1P1CDataflowPerfTest:AbstractUniCast1P1CPerfTest
    {
        private readonly BufferBlock<long> _bufferBlock = new BufferBlock<long>();

        public override long RunPass()
        {
            long tplValue = 0L;
            var c = Task.Factory.StartNew(
                () =>
                    {
                        tplValue = 0L;
                        for (long i = 0; i < Iterations; i++)
                        {
                            long value = _bufferBlock.Receive();
                            tplValue += value;
                        }
                    }
                );

            var sw = Stopwatch.StartNew();

            for (long i = 0; i < Iterations; i++)
            {
                _bufferBlock.Post(i);
            }
            Task.WaitAll(c);

            var opsPerSecond = (Iterations * 1000L) / (sw.ElapsedMilliseconds);

            Assert.AreEqual(ExpectedResult, tplValue, "RunTplDataflowPass");

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}