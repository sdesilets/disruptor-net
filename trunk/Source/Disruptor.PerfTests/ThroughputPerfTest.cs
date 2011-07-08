using System;
using Disruptor.PerfTests.Runner;

namespace Disruptor.PerfTests
{
    public abstract class ThroughputPerfTest : PerformanceTest
    {
        public abstract long RunPass();

        protected override void RunAsUnitTest()
        {
            var operationsPerSecond = RunPass();
            Console.WriteLine("{0}: {1:###,###,###,###}op/sec", GetType().Name, operationsPerSecond);
        }

        public override TestRun CreateTestRun(int pass)
        {
            return new ThroughputTestRun(this, pass);
        }
    }
}