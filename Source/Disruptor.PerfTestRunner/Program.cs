using System;
using Disruptor.PerfTests;

namespace Disruptor.PerfTestRunner
{
    public class Program
    {
        static void Main(string[] args)
        {
            (new Pipeline3StepPerfTest()).ShouldCompareDisruptorVsQueues();
            (new UniCast1P1CPerfTest()).ShouldCompareDisruptorVsQueues();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
