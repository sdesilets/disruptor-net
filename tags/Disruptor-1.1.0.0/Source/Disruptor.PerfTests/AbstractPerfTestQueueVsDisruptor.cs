using System;
using NUnit.Framework;

namespace Disruptor.PerfTests
{
    public abstract class AbstractPerfTestQueueVsDisruptor
    {
        protected void TestImplementations()
        {
            const int runs = 3;
            var disruptorOps = 0L;
            var queueOps = 0L;

            for (var i = 0; i < runs; i++)
            {
                GC.Collect();

                disruptorOps = RunDisruptorPass(i);
                Console.WriteLine("Run disruptor finished");

                queueOps = RunQueuePass(i);
                Console.WriteLine("Run queue finished");

                PrintResults(GetType().Name, disruptorOps, queueOps, i);
            }

            Assert.IsTrue(disruptorOps > queueOps, "Performance degraded");
        }

        public static void PrintResults(string className, long disruptorOps, long queueOps, int i)
        {
            Console.WriteLine("{0} OpsPerSecond run {1}: BlockingQueues={2:N}, Disruptor={3:N}, Ratio=x{4:0.0}", className, i, queueOps, disruptorOps, disruptorOps/(double)queueOps);
        }

        protected abstract long RunQueuePass(int passNumber);

        protected abstract long RunDisruptorPass(int passNumber);

        public abstract void ShouldCompareDisruptorVsQueues();
    }
}