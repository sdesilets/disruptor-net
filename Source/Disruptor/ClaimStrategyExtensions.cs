using System;
using System.Threading;

namespace Disruptor
{
    ///<summary>
    /// Fatory used by the <see cref="RingBuffer{T}"/> to instantiate the selected <see cref="IClaimStrategy"/>.
    ///</summary>
    public static class ClaimStrategyExtensions
    {
        /// <summary>
        /// Used by the <see cref="RingBuffer{T}"/> as a polymorphic constructor.
        /// </summary>
        /// <param name="option">strategy to be used.</param>
        /// <returns>a new instance of the ClaimStrategy</returns>
        public static IClaimStrategy GetInstance(this ClaimStrategyOption option)
        {
            switch (option)
            {
                case ClaimStrategyOption.MultipleProducers:
                    return new MultiThreadedStrategy();
                case ClaimStrategyOption.SingleProducer:
                    return new SingleThreadedStrategy();
                default:
                    throw new InvalidOperationException("Option not supported");
            }
        }

        /// <summary>
        /// Strategy to be used when there are multiple producer threads claiming <see cref="Event{T}"/>s.
        /// </summary>
        private sealed class MultiThreadedStrategy : IClaimStrategy
        {
            private long _sequence = RingBufferConvention.InitialCursorValue;

            public long IncrementAndGet()
            {
                return Interlocked.Increment(ref _sequence);
            }

            public long IncrementAndGet(int delta)
            {
                return Interlocked.Add(ref _sequence, delta);
            }
        }

        /// <summary>
        /// Optimised strategy can be used when there is a single producer thread claiming <see cref="Event{T}"/>s.
        /// </summary>
        private sealed class SingleThreadedStrategy : IClaimStrategy
        {
            private long _sequence = RingBufferConvention.InitialCursorValue;

            public long IncrementAndGet()
            {
                return ++_sequence;
            }

            public long IncrementAndGet(int delta)
            {
                _sequence += delta;
                return _sequence;
            }
        }
    }
}