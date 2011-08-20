using System;
using Disruptor.MemoryLayout;

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
            private CacheLineStorageAtomicLong _sequence = new CacheLineStorageAtomicLong(RingBufferConvention.InitialCursorValue);

            public long IncrementAndGet()
            {
                return _sequence.IncrementAndGet();
            }

            public long IncrementAndGet(int delta)
            {
                return _sequence.IncrementAndGet(delta);
            }
        }

        /// <summary>
        /// Optimised strategy can be used when there is a single producer thread claiming <see cref="Event{T}"/>s.
        /// </summary>
        private sealed class SingleThreadedStrategy : IClaimStrategy
        {
            private CacheLineStorageLong _sequence = new CacheLineStorageLong(RingBufferConvention.InitialCursorValue);

            public long IncrementAndGet()
            {
                return ++_sequence.Data;
            }

            public long IncrementAndGet(int delta)
            {
                _sequence.Data += delta;
                return _sequence.Data;
            }
        }
    }
}