using System;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;

namespace Disruptor.PerfTests.DiamondPath1P3C
{
    [TestFixture]
    public class DiamondPath1P3CDataflowPerfTest:AbstractDiamondPath1P3CPerfTest
    {
        public override long RunPass()
        {
            long tplValue = 0L;
            var bb = new BroadcastBlock<long>(_ => _);
            var jb = new JoinBlock<bool, bool>(new GroupingDataflowBlockOptions() { Greedy = true });
            var ab = new ActionBlock<long>(i => jb.Target1.Post((i % 3L) == 0));
            var ab2 = new ActionBlock<long>(i => jb.Target2.Post((i % 5L) == 0));
            bb.LinkTo(ab);
            bb.LinkTo(ab2);

            var ab3 = new ActionBlock<Tuple<bool, bool>>(t => { if (t.Item1 && t.Item2) ++tplValue; });
            jb.LinkTo(ab3);

            var sw = Stopwatch.StartNew();
            for (long i = 0; i < Iterations; i++) bb.Post(i);
            bb.Complete();
            bb.Completion.Wait();
            ab.Complete();
            ab2.Complete();
            ab.Completion.Wait();
            ab2.Completion.Wait();
            jb.Complete();
            jb.Completion.Wait();
            ab3.Complete();
            ab3.Completion.Wait();

            var opsPerSecond = (Iterations * 1000L) / (sw.ElapsedMilliseconds);
            Assert.AreEqual(ExpectedResult, tplValue);
            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}