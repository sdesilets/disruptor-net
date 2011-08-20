using System;
using System.Threading;
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
        /// <param name="bufferSize"></param>
        /// <returns>a new instance of the ClaimStrategy</returns>
        public static IClaimStrategy GetInstance(this ClaimStrategyOption option, int bufferSize)
        {
            switch (option)
            {
                case ClaimStrategyOption.MultipleProducers:
                    return new MultiThreadedStrategy(bufferSize);
                case ClaimStrategyOption.SingleProducer:
                    return new SingleThreadedStrategy(bufferSize);
                default:
                    throw new InvalidOperationException("Option not supported");
            }
        }

        /// <summary>
        /// Strategy to be used when there are multiple producer threads claiming <see cref="Event{T}"/>s.
        /// </summary>
        private sealed class MultiThreadedStrategy : IClaimStrategy
        {
            private readonly int _bufferSize;
            private CacheLineStorageAtomicLong _sequence = new CacheLineStorageAtomicLong(RingBufferConvention.InitialCursorValue);
            private CacheLineStorageVolatileLong _minProcessorSequence;

            public MultiThreadedStrategy(int bufferSize)
            {
                _bufferSize = bufferSize;
                _minProcessorSequence.Data = RingBufferConvention.InitialCursorValue;
            }

            public long IncrementAndGet()
            {
                return _sequence.IncrementAndGet();
            }

            public void EnsureProcessorsAreInRange(long sequence, Sequence[] dependentSequences)
            {
                var wrapPoint = sequence - _bufferSize;

                if (wrapPoint > _minProcessorSequence.Data)
                {
                    long minSequence;
                    while (wrapPoint > (minSequence = dependentSequences.GetMinimumSequence()))
                    {
                        Thread.Yield();
                    }

                    _minProcessorSequence.Data = minSequence;
                }
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
            private readonly int _bufferSize;
            private CacheLineStorageLong _sequence = new CacheLineStorageLong(RingBufferConvention.InitialCursorValue);
            private CacheLineStorageLong _minProcessorSequence;

            public SingleThreadedStrategy(int bufferSize)
            {
                _bufferSize = bufferSize;
                _minProcessorSequence.Data = RingBufferConvention.InitialCursorValue;
            }

            public long IncrementAndGet()
            {
                return ++_sequence.Data;
            }

            public void EnsureProcessorsAreInRange(long sequence, Sequence[] dependentSequences)
            {
                var wrapPoint = sequence - _bufferSize;

                if (wrapPoint > _minProcessorSequence.Data)
                {
                    long minSequence;
                    while (wrapPoint > (minSequence = dependentSequences.GetMinimumSequence()))
                    {
                        Thread.Yield();
                    }
                    _minProcessorSequence.Data = minSequence;
                }
            }

            public long IncrementAndGet(int delta)
            {
                _sequence.Data += delta;
                return _sequence.Data;
            }
        }
    }
}