using System;
using Disruptor.PerfTests.Runner;

namespace Disruptor.PerfTests
{
    public static class Program
    {
        static void Main(string[] args)
        {
            ScenarioType scenarioType;
            ImplementationType implementationType;
            int runs;
            int iterationsInMillions;

            if (args == null
                || args.Length != 4
                || !Enum.TryParse(args[0], out scenarioType)
                || !Enum.TryParse(args[1], out implementationType)
                || !int.TryParse(args[2], out runs)
                || !int.TryParse(args[3], out iterationsInMillions))
            {
                PrintUsage();
                return;
            }
            
            HostConfiguration.WriteToConsole();

            var session = new PerformanceTestSession(scenarioType, implementationType, runs,
                                                     iterationsInMillions*1000*1000);

            session.Run();

            session.GenerateAndOpenReport();
            Console.ReadKey();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: PerfRunner Scenario Implementation Runs Iterations");
            Console.WriteLine();
            PrintEnum(typeof (ScenarioType));
            Console.WriteLine();
            PrintEnum(typeof(ImplementationType));
            Console.WriteLine();
            Console.WriteLine("Runs: number of test run to do for each scenario and implementation");
            Console.WriteLine("Iterations: number of iterations per run in millions");
            Console.WriteLine();
            Console.WriteLine("Example: PerfRunner 1 1 10");
            Console.WriteLine("will run UniCast1P1C performance test with the disruptor only and will do 10 million iterations.");
        }

        private static void PrintEnum(Type enumType)
        {
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValues(enumType);

            Console.WriteLine(enumType.Name + " options:");

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var value = (int)values.GetValue(i);
                Console.WriteLine(" - {0} ({1})", value, name);
            }
        }
    }
}

