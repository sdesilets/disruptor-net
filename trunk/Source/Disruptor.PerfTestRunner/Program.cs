using System;
using Disruptor.PerfTests;

namespace Disruptor.PerfTestRunner
{
    public static class Program
    {
        static void Main()
        {
            //RunTest(new UniCast1P1CPerfTest());
            RunTest(new Pipeline3StepPerfTest());

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void RunTest(AbstractPerfTestQueueVsDisruptor test)
        {
            try
            {
                test.ShouldCompareDisruptorVsQueues();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown in UniCast1P1CPerfTest:" + e);
                throw;
            }
        }
    }
}
