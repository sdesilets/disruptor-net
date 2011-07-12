using System;
using System.Threading;
using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable entries containing the data representing an <see cref="Entry{T}"/> being exchanged between producers and consumers.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class ValueTypeRingBuffer<T> : ISequencable where T:struct
    {
        private readonly ClaimStrategyFactory.ClaimStrategyOption _claimStrategyOption;
        private CacheLineStorageLong _cursor = new CacheLineStorageLong(RingBufferConvention.InitialCursorValue);
        private readonly Entry<T>[] _entries;
        private readonly int _ringModMask;
        private readonly IClaimStrategy _claimStrategy;
        private readonly IWaitStrategy _waitStrategy;

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// Use this constructor is T is a value type, otherwise use the other constructor which require a factory to fill the <see cref="RingBuffer{T}"/> with pre-allocated instances of T
        /// </summary>
        /// <param name="size">size of the RingBuffer that will be rounded up to the next power of 2</param>
        /// <param name="claimStrategyOption"> threading strategy for producers claiming <see cref="Entry{T}"/>s in the ring.</param>
        /// <param name="waitStrategyOption">waiting strategy employed by consumers waiting on <see cref="Entry{T}"/>s becoming available.</param>
        public ValueTypeRingBuffer(int size, ClaimStrategyFactory.ClaimStrategyOption claimStrategyOption = ClaimStrategyFactory.ClaimStrategyOption.Multithreaded, WaitStrategyFactory.WaitStrategyOption waitStrategyOption = WaitStrategyFactory.WaitStrategyOption.Blocking)
        {
            _claimStrategyOption = claimStrategyOption;
            var sizeAsPowerOfTwo = Util.CeilingNextPowerOfTwo(size);
            _ringModMask = sizeAsPowerOfTwo - 1;
            _entries = new Entry<T>[sizeAsPowerOfTwo];

            _claimStrategy = ClaimStrategyFactory.GetInstance(claimStrategyOption);
            _waitStrategy = WaitStrategyFactory.GetInstance(waitStrategyOption);
        }

        /// <summary>
        ///  Create a <see cref="IConsumerBarrier{T}"/> that gates on the RingBuffer and a list of <see cref="IConsumer"/>s
        /// </summary>
        /// <param name="consumersToTrack">consumersToTrack this barrier will track</param>
        /// <returns>the barrier gated as required</returns>
        public IConsumerBarrier<T> CreateConsumerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ValueTypeConsumerTrackingConsumerBarrier<T>(this, consumersToTrack);
        }

        /// <summary>
        /// Create a <see cref="IValueTypeProducerBarrier{T}"/> on this RingBuffer that tracks dependent <see cref="IConsumer"/>s.
        /// </summary>
        /// <param name="consumersToTrack"></param>
        /// <returns></returns>
        public IValueTypeProducerBarrier<T> CreateProducerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ValueTypeConsumerTrackingProducerBarrier(this, consumersToTrack);
        }

        /// <summary>
        /// The capacity of the RingBuffer to hold entries.
        /// </summary>
        public int Capacity
        {
            get { return _entries.Length; }
        }

        /// <summary>
        /// Get the current sequence that producers have committed to the RingBuffer.
        /// </summary>
        public long Cursor
        {
            get { return _cursor.Data; }
            private set{ _cursor.Data = value;}
        }

        ///<summary>
        /// Get the <see cref="Entry{T}"/> for a given sequence in the RingBuffer.
        ///</summary>
        ///<param name="sequence">sequence for the <see cref="Entry{T}"/></param>
        public Entry<T> this[long sequence]
        {
            get
            {
                return _entries[(int)sequence & _ringModMask];
            }
        }

        /// <summary>
        /// ConsumerBarrier handed out for gating consumers of the RingBuffer and dependent <see cref="IConsumer"/>(s)
        /// </summary>
        /// <typeparam name="TU"></typeparam>
        private sealed class ValueTypeConsumerTrackingConsumerBarrier<TU>
            : IConsumerBarrier<TU> where TU : struct
        {
            private readonly IConsumer[] _consumers;
            private volatile bool _alerted;
            private readonly ValueTypeRingBuffer<TU> _ringBuffer;

            public ValueTypeConsumerTrackingConsumerBarrier(ValueTypeRingBuffer<TU> ringBuffer, params IConsumer[] consumers)
            {
                _ringBuffer = ringBuffer;
                _consumers = consumers;
            }

            public TU GetEntry(long sequence)
            {
                return _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask].Data;
            }

            public long? WaitFor(long sequence)
            {
                return _ringBuffer._waitStrategy.WaitFor(_consumers, _ringBuffer, this, sequence);
            }

            public long? WaitFor(long sequence, TimeSpan timeout)
            {
                return _ringBuffer._waitStrategy.WaitFor(_consumers, _ringBuffer, this, sequence, timeout);
            }

            public long Cursor
            {
                get { return _ringBuffer.Cursor; }
            }

            public bool IsAlerted
            {
                get { return _alerted; }
            }

            public void Alert()
            {
                _alerted = true;
                _ringBuffer._waitStrategy.SignalAll();
            }

            public void ClearAlert()
            {
                _alerted = false;
            }
        }

        /// <summary>
        /// <see cref="IValueTypeProducerBarrier{T}"/> that tracks multiple <see cref="IConsumer"/>s when trying to claim
        /// a <see cref="Entry{T}"/> in the <see cref="RingBuffer{T}"/>.
        /// </summary>
        private sealed class ValueTypeConsumerTrackingProducerBarrier : IValueTypeProducerBarrier<T>
        {
            private readonly ValueTypeRingBuffer<T> _ringBuffer;
            private readonly IConsumer[] _consumers;
            private long _lastConsumerMinimum = RingBufferConvention.InitialCursorValue;

            public ValueTypeConsumerTrackingProducerBarrier(ValueTypeRingBuffer<T> ringBuffer, params IConsumer[] consumers)
            {
                if (consumers.Length == 0)
                {
                    throw new ArgumentException("There must be at least one Consumer to track for preventing ring wrap");
                }

                _ringBuffer = ringBuffer;
                _consumers = consumers;
            }

            public void Commit(T data)
            {
                var sequence = _ringBuffer._claimStrategy.IncrementAndGet();
                EnsureConsumersAreInRange(sequence);

                _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask] = new Entry<T>(sequence, data);

                if (_ringBuffer._claimStrategyOption == ClaimStrategyFactory.ClaimStrategyOption.Multithreaded)
                {
                    var sequenceMinusOne = sequence - 1;
                    while (sequenceMinusOne != _ringBuffer.Cursor) // volatile read
                    {
                        //busy spin
                    }
                }

                _ringBuffer.Cursor = sequence; // volatile write
                _ringBuffer._waitStrategy.SignalAll();
            }

            public long Cursor
            {
                get { return _ringBuffer.Cursor; }
            }

            private void EnsureConsumersAreInRange(long sequence)
            {
                var wrapPoint = sequence - _ringBuffer._entries.Length;
                while (wrapPoint > _lastConsumerMinimum && wrapPoint > (_lastConsumerMinimum = _consumers.GetMinimumSequence()))
                {
                    Thread.Yield();
                }
            }
        }
    }
}