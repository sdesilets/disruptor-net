using System;
using Disruptor.PerfTests;

namespace Disruptor.PerfTestRunner
{
    public static class Program
    {
        static void Main()
        {
            RunTest(new UniCast1P1CPerfTest());
            RunTest(new Pipeline3StepPerfTest());
            RunLatencyTest();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void RunLatencyTest()
        {
            try
            {
                new Pipeline3StepLatencyPerfTest().ShouldCompareDisruptorVsQueues();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown in Pipeline3StepLatencyPerfTest:{0}", e);
            }
        }

        private static void RunTest(AbstractPerfTestQueueVsDisruptor test)
        {
            try
            {
                test.ShouldCompareDisruptorVsQueues();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown in {0}:{1}",test.GetType().Name, e);
                throw;
            }
        }
    }
}
