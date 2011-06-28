using System;
using Disruptor.PerfTests;

namespace Disruptor.PerfTestRunner
{
    public class Program
    {
        static void Main(string[] args)
        {
            var uniCast1P1CPerfTest = new UniCast1P1CPerfTest(); 
            uniCast1P1CPerfTest.ShouldCompareDisruptorVsQueues();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
