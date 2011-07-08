using System.Text;

namespace Disruptor.PerfTests.Runner
{
    public abstract class TestRun
    {
        protected TestRun(int run)
        {
            RunIndex = run;
        }

        public int RunIndex { get; private set; }
        public abstract void Run();
        public long DurationInMs { get; set; }
        public int Gen0Count { get; set; }
        public int Gen1Count { get; set; }
        public int Gen2Count { get; set; }

        public abstract void AppendResultHtml(StringBuilder sb);
    }
}