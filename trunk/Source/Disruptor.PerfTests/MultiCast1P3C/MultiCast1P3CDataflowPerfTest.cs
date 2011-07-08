using NUnit.Framework;

namespace Disruptor.PerfTests.MultiCast1P3C
{
    [TestFixture]
    [Ignore("To implement")]
    public class MultiCast1P3CDataflowPerfTest:AbstractMultiCast1P3CPerfTest
    {
        public override long RunPass()
        {
            return 0L;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}