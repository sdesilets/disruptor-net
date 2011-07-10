using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3StepLatency
{
    [TestFixture]
    public class Pipeline3StepLatencyDataflowPerfTest : AbstractPipeline3StepLatencyPerfTest
    {
        public override void RunPass()
        {
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}