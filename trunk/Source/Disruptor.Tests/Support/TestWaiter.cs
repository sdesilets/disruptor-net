using System.Collections.Generic;
using System.Threading;

namespace Disruptor.Tests.Support
{
    public class TestWaiter
    {
        private readonly Barrier _barrier;
        private readonly IConsumerBarrier<StubData> _consumerBarrier;
        private readonly long _initialSequence;
        private readonly long _toWaitForSequence;

        public TestWaiter(Barrier barrier, IConsumerBarrier<StubData> consumerBarrier, long initialSequence, long toWaitForSequence)
        {
            _barrier = barrier;
            _consumerBarrier = consumerBarrier;
            _initialSequence = initialSequence;
            _toWaitForSequence = toWaitForSequence;
        }

        public List<StubData> Call()
        {
            _barrier.SignalAndWait();
            _consumerBarrier.WaitFor(_toWaitForSequence);

            var messages = new List<StubData>();
            for (var l = _initialSequence; l <= _toWaitForSequence; l++)
            {
                messages.Add(_consumerBarrier.GetEntry(l));
            }

            return messages;
        }
    }
}