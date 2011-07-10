using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.PerfTests.Runner
{
    public class Implementation
    {
        private readonly string _scenarioName;
        private readonly string _implementationName;
        private readonly IList<TestRun> _runTestReport = new List<TestRun>();
        
        public Implementation(string scenarioName, string implementationName, int runs, int iterations)
        {
            _scenarioName = scenarioName;
            _implementationName = implementationName;
            var ns = "Disruptor.PerfTests." + scenarioName;
            var className = scenarioName + implementationName + "PerfTest";
            var classFullName = ns + "." + className;

            var perfTestType = Type.GetType(classFullName);
            
            for (int i = 0; i < runs; i++)
            {
                var perfTest = (PerformanceTest)Activator.CreateInstance(perfTestType);
                // perfTest.Iterations = iterations; //TODO change the logic to be able to define iterations
                _runTestReport.Add(perfTest.CreateTestRun(i));
            }
        }

        public void Run()
        {
            foreach (var testRun in _runTestReport)
            {
                testRun.Run();
            }
        }

        public void AppendDetailedHtmlReport(StringBuilder sb)
        {
            foreach (var testRun in _runTestReport)
            {
                sb.AppendLine("            <tr>");
                sb.AppendLine("                <td>" + _scenarioName + "</td>");
                sb.AppendLine("                <td>" + _implementationName + "</td>");
                sb.AppendLine("                <td>" + testRun.RunIndex + "</td>");
                testRun.AppendResultHtml(sb);
                sb.AppendLine("                <td>" + testRun.DurationInMs + "(ms)</td>");
                sb.AppendLine(string.Format("                <td>{0}-{1}-{2}</td>", testRun.Gen0Count, testRun.Gen1Count, testRun.Gen2Count));
                sb.AppendLine("            </tr>");
            }
        }
    }
}