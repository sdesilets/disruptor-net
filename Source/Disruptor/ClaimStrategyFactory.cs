using System;
using System.Threading;

namespace Disruptor
{
    public static class ClaimStrategyFactory
    {
        public enum ClaimStrategyOption
        {
            Multithreaded,
            SingleThreaded
        }

        public static IClaimStrategy GetInstance(ClaimStrategyOption option)
        {
            switch (option)
            {
                case ClaimStrategyOption.Multithreaded:
                    return new MultiThreadedStrategy();
                case ClaimStrategyOption.SingleThreaded:
                    return new SingleThreadedStrategy();
                default:
                    throw new InvalidOperationException("Option not supported");
            }
        }

        /// <summary>
        /// Strategy to be used when there are multiple producer threads claiming <see cref="IEntry"/>s.
        /// </summary>
        private sealed class MultiThreadedStrategy : IClaimStrategy
        {
            private long _sequence;

            public long GetAndIncrement()
            {
                Interlocked.Increment(ref _sequence);
                return _sequence;
            }

            public void SetSequence(long sequence)
            {
                //TODO review
                Interlocked.Exchange(ref _sequence, sequence);
            }

            public void WaitForCursor<T>(long sequence, RingBuffer<T> ringBuffer) where T:IEntry
            {
                while (ringBuffer.Cursor != sequence)
                {
                    // busy spin
                }
            }
        }

        /// <summary>
        /// Optimised strategy can be used when there is a single producer thread claiming <see cref="IEntry"/>s.
        /// </summary>
        private sealed class SingleThreadedStrategy : IClaimStrategy
        {
            private long _sequence;

            public long GetAndIncrement()
            {
                return _sequence++;
            }

            public void SetSequence(long sequence)
            {
                _sequence = sequence;
            }

            public void WaitForCursor<T>(long sequence, RingBuffer<T> ringBuffer) where T:IEntry
            {
                // no op when on a single producer.
            }
        }
    }
}