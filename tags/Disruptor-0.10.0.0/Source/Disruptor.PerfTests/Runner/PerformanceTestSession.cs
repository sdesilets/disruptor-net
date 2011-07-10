using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Disruptor.PerfTests.Runner
{
    public class PerformanceTestSession
    {
        private readonly ScenarioType _scenarioType;
        private readonly ImplementationType _implementationType;
        private readonly int _runs;
        private readonly int _iterations;
        private readonly IList<Scenario> _scenarios = new List<Scenario>();

        public PerformanceTestSession(ScenarioType scenarioType, ImplementationType implementationType, int runs, int iterations)
        {
            _scenarioType = scenarioType;
            _implementationType = implementationType;
            _runs = runs;
            _iterations = iterations;
            Console.WriteLine("Scenario={0}, Implementation={1}, Runs={2}, Iterations={3:###,###,###,###}", scenarioType, implementationType, runs, iterations);

            if (scenarioType == ScenarioType.All)
            {
                foreach (var scenarioName in Enum.GetNames(typeof(ScenarioType)).Where(s => s != "All"))
                {
                    _scenarios.Add(new Scenario(scenarioName, implementationType, runs, iterations));
                }
            }
            else
            {
                string scenarioName = scenarioType.ToString();
                _scenarios.Add(new Scenario(scenarioName, implementationType, runs, iterations));
            }
        }

        public void Run()
        {
            foreach (var scenario in _scenarios)
            {
                scenario.Run();
            }
        }

        private string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">")
                .AppendLine("<html>")
                .AppendLine("	<head>")
                .AppendLine("		<title>Disruptor-net - Test Report</title>")
                .AppendLine("	</head>")
                .AppendLine("	<body>")
                .AppendLine("        Local time: " + DateTime.Now + "<br>")
                .AppendLine("        UTC time: " + DateTime.UtcNow);

            sb.AppendLine("        <h2>Host configuration</h2>");
            HostConfiguration.AppendConfigurationInHtml(sb);

            sb.AppendLine("        <h2>Test configuration</h2>")
                .AppendLine("        Scenarios: " + _scenarioType + "<br>")
                .AppendLine("        Implementations: " + _implementationType + "<br>")
                .AppendLine("        Runs: " + _runs + "<br>")
                .AppendLine("        Iterations: " + _iterations + "<br>");

            sb.AppendLine("        <h2>Test results</h2>")
                .AppendLine("        Best results of " + _runs + " run(s).<br>")
                .AppendLine("        <br>");

            //TODO
            sb.AppendLine("        <h2>Detailed test results</h2>");
            sb.AppendLine("        <table border=\"1\">");
            sb.AppendLine("            <tr>");
            sb.AppendLine("                <td>Scenario</td>");
            sb.AppendLine("                <td>Implementation</td>");
            sb.AppendLine("                <td>Run</td>");
            sb.AppendLine("                <td>Operations per second</td>");
            sb.AppendLine("                <td>Duration (ms)</td>");
            sb.AppendLine("                <td># GC (0-1-2)</td>");
            sb.AppendLine("            </tr>");

            foreach (var scenario in _scenarios)
            {
                scenario.AppendDetailedHtmlReport(sb);
            }

            sb.AppendLine("        </table>");

            return sb.ToString();
        }

        public void GenerateAndOpenReport()
        {
            var path = Path.Combine(Environment.CurrentDirectory,
                                    "TestReport-" + DateTime.UtcNow.ToString("yyyy-MM-dd hh-mm-ss") + ".html");

            File.WriteAllText(path, BuildReport());

            Process.Start(path);
        }
    }
}