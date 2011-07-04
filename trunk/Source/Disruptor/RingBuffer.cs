using System;
using System.Threading;
using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable entries containing the data representing an <see cref="Entry{T}"/> being exchanged between producers and consumers.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class RingBuffer<T> : ISequencable where T : class 
    {
        private readonly ClaimStrategyFactory.ClaimStrategyOption _claimStrategyOption;

        /// <summary>
        /// Initial cursor value, set to -1 as sequence starting point 
        /// </summary>
        public const long InitialCursorValue = -1L;

        private CacheLineStorageLong _cursor = new CacheLineStorageLong(InitialCursorValue);
        private readonly Entry<T>[] _entries;
        private readonly int _ringModMask;
        private readonly IClaimStrategy _claimStrategy;
        private readonly IWaitStrategy _waitStrategy;

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// </summary>
        /// <param name="entryFactory"> entryFactory to create instances of T for filling the RingBuffer</param>
        /// <param name="size">size of the RingBuffer that will be rounded up to the next power of 2</param>
        /// <param name="claimStrategyOption"> threading strategy for producers claiming entries in the ring.</param>
        /// <param name="waitStrategyOption">waiting strategy employed by consumers waiting on entries becoming available.</param>
        public RingBuffer(Func<T> entryFactory, int size, ClaimStrategyFactory.ClaimStrategyOption claimStrategyOption = ClaimStrategyFactory.ClaimStrategyOption.Multithreaded, WaitStrategyFactory.WaitStrategyOption waitStrategyOption = WaitStrategyFactory.WaitStrategyOption.Blocking)
        {
            _claimStrategyOption = claimStrategyOption;
            var sizeAsPowerOfTwo = Util.CeilingNextPowerOfTwo(size);
            _ringModMask = sizeAsPowerOfTwo - 1;
            _entries = new Entry<T>[sizeAsPowerOfTwo];

            _claimStrategy = ClaimStrategyFactory.GetInstance(claimStrategyOption);
            _waitStrategy = WaitStrategyFactory.GetInstance(waitStrategyOption);

            Fill(entryFactory);
        }

        /// <summary>
        ///  Create a <see cref="IConsumerBarrier{T}"/> that gates on the RingBuffer and a list of <see cref="IConsumer"/>s
        /// </summary>
        /// <param name="consumersToTrack">consumersToTrack this barrier will track</param>
        /// <returns>the barrier gated as required</returns>
        public IConsumerBarrier<T> CreateConsumerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ConsumerTrackingConsumerBarrier<T>(this, consumersToTrack);
        }

        /// <summary>
        /// Create a <see cref="IReferenceTypeProducerBarrier{T}"/> on this RingBuffer that tracks dependent <see cref="IConsumer"/>s.
        /// </summary>
        /// <param name="consumersToTrack"></param>
        /// <returns></returns>
        public IReferenceTypeProducerBarrier<T> CreateProducerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ReferenceTypeConsumerTrackingProducerBarrier(this, consumersToTrack);
        }

        /// <summary>
        /// Create a <see cref="IReferenceTypeForceFillProducerBarrier{T}"/> on this RingBuffer that tracks dependent <see cref="IConsumer"/>s.
        /// This barrier is to be used for filling a RingBuffer when no other producers exist. 
        /// </summary>
        /// <param name="consumersToTrack">consumersToTrack to be tracked to prevent wrapping.</param>
        /// <returns>a <see cref="IReferenceTypeForceFillProducerBarrier{T}"/> with the above configuration.</returns>
        public IReferenceTypeForceFillProducerBarrier<T> CreateForceFillProducerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ReferenceTypeForceFillConsumerTrackingProducerBarrier(this, consumersToTrack);
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

        private void Fill(Func<T> entryEntryFactory)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                var data = entryEntryFactory();
                _entries[i] = new Entry<T>(-1, data);
            }
        }

        /// <summary>
        /// ConsumerBarrier handed out for gating consumers of the RingBuffer and dependent <see cref="IConsumer"/>(s)
        /// </summary>
        /// <typeparam name="TU"></typeparam>
        private sealed class ConsumerTrackingConsumerBarrier<TU> : IConsumerBarrier<TU> where TU : class
        {
            public CacheLinePadding CacheLinePadding1;
            private readonly IConsumer[] _consumers;
            private volatile bool _alerted;
            public CacheLinePadding CacheLinePadding2;
            private readonly RingBuffer<TU> _ringBuffer;

            public ConsumerTrackingConsumerBarrier(RingBuffer<TU> ringBuffer, params IConsumer[] consumers)
            {
                _ringBuffer = ringBuffer;
                _consumers = consumers;

                // to make compiler happy
                CacheLinePadding1 = new CacheLinePadding();
                CacheLinePadding2 = new CacheLinePadding();
            }

            public TU GetEntry(long sequence)
            {
                return _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask].Data;
            }

            public long WaitFor(long sequence)
            {
                return _ringBuffer._waitStrategy.WaitFor(_consumers, _ringBuffer, this, sequence);
            }

            public long WaitFor(long sequence, TimeSpan timeout)
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
        /// <see cref="IReferenceTypeProducerBarrier{T}"/> that tracks multiple <see cref="IConsumer"/>s when trying to claim
        /// a <see cref="Entry{T}"/> in the <see cref="RingBuffer{T}"/>.
        /// </summary>
        private sealed class ReferenceTypeConsumerTrackingProducerBarrier  : IReferenceTypeProducerBarrier<T> 
        {
            private readonly RingBuffer<T> _ringBuffer;
            private readonly IConsumer[] _consumers;
            private long _lastConsumerMinimum;

            public ReferenceTypeConsumerTrackingProducerBarrier(RingBuffer<T> ringBuffer, params IConsumer[] consumers)
            {
                if (consumers.Length == 0)
                {
                    throw new ArgumentException("There must be at least one Consumer to track for preventing ring wrap");
                }

                _ringBuffer = ringBuffer;
                _consumers = consumers;
            }

            public long NextEntry(out T data)
            {
                var sequence = _ringBuffer._claimStrategy.GetAndIncrement();
                EnsureConsumersAreInRange(sequence);

                data = _ringBuffer._entries[(int) sequence & _ringBuffer._ringModMask].Data;

                return sequence;
            }

            public void Commit(long sequence)
            {
                if (_ringBuffer._claimStrategyOption == ClaimStrategyFactory.ClaimStrategyOption.Multithreaded)
                {
                    var sequenceMinusOne = sequence - 1;
                    while (sequenceMinusOne != Cursor)
                    {
                        //busy spin
                    }
                }

                _ringBuffer.Cursor = sequence;
                _ringBuffer._waitStrategy.SignalAll();
            }

            public long Cursor
            {
                get { return _ringBuffer.Cursor; }
            }

            private void EnsureConsumersAreInRange(long sequence)
            {
                var wrapPoint = sequence - _ringBuffer._entries.Length;
                
                while (wrapPoint >= _lastConsumerMinimum && wrapPoint >= (_lastConsumerMinimum = _consumers.GetMinimumSequence()))
                {
                    Thread.Yield();
                }
            }
        }

        /// <summary>
        /// <see cref="IReferenceTypeForceFillProducerBarrier{T}"/> that tracks multiple <see cref="IConsumer"/>s when trying to claim
        ///  a <see cref="Entry{T}"/> in the <see cref="RingBuffer{T}"/>.
        /// </summary>
        private sealed class ReferenceTypeForceFillConsumerTrackingProducerBarrier : IReferenceTypeForceFillProducerBarrier<T>
        {
            private readonly RingBuffer<T> _ringBuffer;
            private readonly IConsumer[] _consumers;
            private long _lastConsumerMinimum;

            public ReferenceTypeForceFillConsumerTrackingProducerBarrier(RingBuffer<T> ringBuffer, params IConsumer[] consumers)
            {
                if (consumers.Length == 0)
                {
                    throw new ArgumentException("There must be at least one Consumer to track for preventing ring wrap");
                }

                _ringBuffer = ringBuffer;
                _consumers = consumers;
            }

            public T ClaimEntry(long sequence)
            {
                EnsureConsumersAreInRange(sequence);

                var entry = _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask];

                return entry.Data;
            }

            public void Commit(long sequence)
            {
                _ringBuffer._claimStrategy.SetSequence(sequence + 1L);
                _ringBuffer.Cursor = sequence;
                _ringBuffer._waitStrategy.SignalAll();
            }

            public long Cursor
            {
                get
                {
                    return _ringBuffer.Cursor;    
                }
            }

            private void EnsureConsumersAreInRange(long sequence)
            {
                var wrapPoint = sequence - _ringBuffer._entries.Length;

                while (wrapPoint >= _lastConsumerMinimum && wrapPoint >= (_lastConsumerMinimum = _consumers.GetMinimumSequence()))
                {
                    Thread.Yield();
                }
            }
        }
    }
}