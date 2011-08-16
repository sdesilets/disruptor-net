using System.Collections.Generic;
using System.Threading;

namespace Disruptor.Tests.Support
{
    public class TestWaiter
    {
        private readonly Barrier _barrier;
        private readonly IDependencyBarrier<StubData> _dependencyBarrier;
        private readonly long _initialSequence;
        private readonly long _toWaitForSequence;

        public TestWaiter(Barrier barrier, IDependencyBarrier<StubData> dependencyBarrier, long initialSequence, long toWaitForSequence)
        {
            _barrier = barrier;
            _dependencyBarrier = dependencyBarrier;
            _initialSequence = initialSequence;
            _toWaitForSequence = toWaitForSequence;
        }

        public List<StubData> Call()
        {
            _barrier.SignalAndWait();
            _dependencyBarrier.WaitFor(_toWaitForSequence);

            var events = new List<StubData>();
            for (var l = _initialSequence; l <= _toWaitForSequence; l++)
            {
                events.Add(_dependencyBarrier.GetEvent(l));
            }

            return events;
        }
    }
}