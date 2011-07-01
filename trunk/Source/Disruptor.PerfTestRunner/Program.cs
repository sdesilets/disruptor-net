using System;
using Disruptor.PerfTests;

namespace Disruptor.PerfTestRunner
{
    public static class Program
    {
        static void Main()
        {
            try
            {
                (new UniCast1P1CPerfTest()).ShouldCompareDisruptorVsQueues();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown in UniCast1P1CPerfTest:" + e);
                throw;
            }

            //(new Pipeline3StepPerfTest()).ShouldCompareDisruptorVsQueues();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
