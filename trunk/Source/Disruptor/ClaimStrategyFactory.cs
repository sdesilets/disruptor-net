using System;
using System.Threading;

namespace Disruptor
{
    ///<summary>
    /// Fatory used by the <see cref="RingBuffer{T}"/> to instantiate the selected <see cref="IClaimStrategy"/>.
    ///</summary>
    public static class ClaimStrategyFactory
    {
        /// <summary>
        /// Indicates the threading policy to be applied for claiming <see cref="Entry{T}"/>s by producers to the <see cref="RingBuffer{T}"/>.
        /// </summary>
        public enum ClaimStrategyOption
        {
            /// <summary>
            /// Strategy to be used when there are multiple producer threads claiming <see cref="Entry{T}"/>s.
            /// </summary>
            Multithreaded,
            /// <summary>
            /// Optimised strategy can be used when there is a single producer thread claiming <see cref="Entry{T}"/>s.
            /// </summary>
            SingleThreaded
        }

        /// <summary>
        /// Used by the <see cref="RingBuffer{T}"/> as a polymorphic constructor.
        /// </summary>
        /// <param name="option">strategy to be used.</param>
        /// <returns>a new instance of the ClaimStrategy</returns>
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
        /// Strategy to be used when there are multiple producer threads claiming <see cref="Entry{T}"/>s.
        /// </summary>
        private sealed class MultiThreadedStrategy : IClaimStrategy
        {
            private long _sequence;

            public long GetAndIncrement()
            {
                var value = Interlocked.Increment(ref _sequence);
                return value - 1;
            }

            public void SetSequence(long sequence)
            {
                Interlocked.Exchange(ref _sequence, sequence);
            }
        }

        /// <summary>
        /// Optimised strategy can be used when there is a single producer thread claiming <see cref="Entry{T}"/>s.
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
        }
    }
}