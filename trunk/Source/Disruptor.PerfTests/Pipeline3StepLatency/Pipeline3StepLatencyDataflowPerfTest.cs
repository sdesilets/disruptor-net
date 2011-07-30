using NUnit.Framework;

namespace Disruptor.PerfTests.Pipeline3StepLatency
{
    [TestFixture]
    public class Pipeline3StepLatencyDataflowPerfTest : AbstractPipeline3StepLatencyPerfTest
    {
        public Pipeline3StepLatencyDataflowPerfTest()
            : base(1 * Million)
        {
        }

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