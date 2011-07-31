using System;
using System.Text;

namespace Disruptor.PerfTests.Runner
{
    public static class HostConfiguration
    {
        public static void WriteToConsole()
        {
            Console.WriteLine();
            Console.WriteLine("CONFIGURATION:");
            Console.WriteLine("Process Mode: {0}bits", Environment.Is64BitProcess ? "64" : "32");
            Console.WriteLine("OS: {0} ({1}bits)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "64" : "32");
            Console.WriteLine("Logical cores: {0}", Environment.ProcessorCount);
            
            //TODO use WMI to get more informations (hyperthreading ON/OFF, number of physical cores, architecture, etc)
        }

        public static void AppendConfigurationInHtml(StringBuilder sb)
        {
            sb.AppendFormat("Process Mode: {0}bits<br>", Environment.Is64BitProcess ? "64" : "32");
            sb.AppendLine();
            sb.AppendFormat("OS: {0} ({1}bits)<br>", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "64" : "32");
            sb.AppendLine();
            sb.AppendFormat("Logical cores: {0}<br>", Environment.ProcessorCount);
            sb.AppendLine();
        }
    }
}