using System;
using Disruptor.PerfTests;

namespace Disruptor.PerfTestRunner
{
    public static class Program
    {
        static void Main()
        {
            PrintProcessMode();

            RunTest(new UniCast1P1CPerfTest());
            //RunTest(new Pipeline3StepPerfTest());
            //RunTest(new DiamondPath1P3CPerfTest());
            //RunTest(new MultiCast1P3CPerfTest());
            //RunTest(new Sequencer3P1CPerfTest());
            //RunLatencyTest();

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

        private static void RunTest(AbstractPerfTestQueueVsDisruptorVsTplDataflow test)
        {
            try
            {
                test.ShouldCompareDisruptorVsQueues();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown in {0}:{1}", test.GetType().Name, e);
                throw;
            }
        }

        private static void PrintProcessMode()
        {
            Console.WriteLine("Process running in {0} bits mode.", Environment.Is64BitProcess ? "64" : "32");
        }
    }
}

