using Disruptor.PerfTests.Runner;

namespace Disruptor.PerfTests
{
    public abstract class PerformanceTest
    {
        public PerformanceTest()
        {
            Iterations = 50*1000*1000;
        }

        public int PassNumber { get; set; }

        public int Iterations { get; set; }
        protected abstract void RunAsUnitTest();
        public abstract void RunPerformanceTest();
        public abstract TestRun CreateTestRun(int pass);
    }
}