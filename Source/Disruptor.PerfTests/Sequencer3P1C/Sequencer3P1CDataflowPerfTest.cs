using NUnit.Framework;

namespace Disruptor.PerfTests.Sequencer3P1C
{
    [TestFixture]
    public class Sequencer3P1CDataflowPerfTest : AbstractSequencer3P1CPerfTest
    {
        public override long RunPass()
        {
            return 0;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}