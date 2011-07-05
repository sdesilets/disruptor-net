using System;
using NUnit.Framework;

namespace Disruptor.PerfTests
{
    public abstract class AbstractPerfTestQueueVsDisruptorVsTplDataflow
    {
        protected void TestImplementations()
        {
            const int runs = 3;
            var disruptorOps = 0L;
            var queueOps = 0L;
            var tplDataflowOps = 0L;

            for (var i = 0; i < runs; i++)
            {
                GC.Collect();

                SetUp(i);

                disruptorOps = RunDisruptorPass(i);
                Console.WriteLine("Run disruptor finished");

                queueOps = RunQueuePass(i);
                Console.WriteLine("Run queue finished");

                tplDataflowOps = RunTplDataflowPass(i);
                Console.WriteLine("Run TPL Dataflow finished");

                PrintResults(GetType().Name, disruptorOps, queueOps, tplDataflowOps, i);
            }

            Assert.IsTrue(disruptorOps > queueOps, "Performance degraded (Queue vs Disruptor)");
            Assert.IsTrue(disruptorOps > tplDataflowOps, "Performance degraded (BufferBlock vs Disruptor)");
        }

        public static void PrintResults(string className, long disruptorOps, long queueOps, long tplDtaflowOps, int i)
        {
            Console.WriteLine("{0} OpsPerSecond run {1}: BlockingQueues={2:N}, Disruptor={3:N}, TPLDataFlow={4:N}, QueueDisruptorRatio=x{5:0.0}", className, i, queueOps, disruptorOps, tplDtaflowOps, disruptorOps / (double)queueOps);
        }

        protected abstract long RunQueuePass(int passNumber);

        protected abstract long RunDisruptorPass(int passNumber);

        protected abstract long RunTplDataflowPass(int passNumber);

        protected abstract void SetUp(int passNumber);

        public abstract void ShouldCompareDisruptorVsQueues();
    }
}
