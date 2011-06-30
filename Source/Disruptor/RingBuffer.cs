using System;
using System.Threading;
using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable entries containing the data representing an <see cref="Entry{T}"/> being exchanged between producers and consumers.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class RingBuffer<T>
    {
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
        /// Use this constructor is T is a value type, otherwise use the other constructor which require a factory to fill the <see cref="RingBuffer{T}"/> with pre-allocated instances of T
        /// </summary>
        /// <param name="size">size of the RingBuffer that will be rounded up to the next power of 2</param>
        /// <param name="claimStrategyOption"> threading strategy for producers claiming <see cref="Entry{T}"/>s in the ring.</param>
        /// <param name="waitStrategyOption">waiting strategy employed by consumers waiting on <see cref="Entry{T}"/>s becoming available.</param>
        public RingBuffer(int size, ClaimStrategyFactory.ClaimStrategyOption claimStrategyOption = ClaimStrategyFactory.ClaimStrategyOption.Multithreaded, WaitStrategyFactory.WaitStrategyOption waitStrategyOption = WaitStrategyFactory.WaitStrategyOption.Blocking)
        {
            if(!typeof(T).IsValueType)
            {
                throw new InvalidOperationException(
                    "This constructor should be used only when T is a value type (struct)");
            }

            var sizeAsPowerOfTwo = Util.CeilingNextPowerOfTwo(size);
            _ringModMask = sizeAsPowerOfTwo - 1;
            _entries = new Entry<T>[sizeAsPowerOfTwo];

            _claimStrategy = ClaimStrategyFactory.GetInstance(claimStrategyOption);
            _waitStrategy = WaitStrategyFactory.GetInstance(waitStrategyOption);
        }

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// </summary>
        /// <param name="entryFactory"> entryFactory to create instances of T for filling the RingBuffer</param>
        /// <param name="size">size of the RingBuffer that will be rounded up to the next power of 2</param>
        /// <param name="claimStrategyOption"> threading strategy for producers claiming entries in the ring.</param>
        /// <param name="waitStrategyOption">waiting strategy employed by consumers waiting on entries becoming available.</param>
        public RingBuffer(Func<T> entryFactory, int size, ClaimStrategyFactory.ClaimStrategyOption claimStrategyOption = ClaimStrategyFactory.ClaimStrategyOption.Multithreaded, WaitStrategyFactory.WaitStrategyOption waitStrategyOption = WaitStrategyFactory.WaitStrategyOption.Blocking)
        {
            if (typeof(T).IsValueType)
            {
                throw new InvalidOperationException(
                    "This constructor should be used only when T is a reference type (class, array, etc)");
            }

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
        /// Create a <see cref="IProducerBarrier{T}"/> on this RingBuffer that tracks dependent <see cref="IConsumer"/>s.
        /// </summary>
        /// <param name="consumersToTrack"></param>
        /// <returns></returns>
        public IProducerBarrier<T> CreateProducerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ConsumerTrackingProducerBarrier(this, consumersToTrack);
        }

        /// <summary>
        /// Create a <see cref="IForceFillProducerBarrier{T}"/> on this RingBuffer that tracks dependent <see cref="IConsumer"/>s.
        /// This barrier is to be used for filling a RingBuffer when no other producers exist. 
        /// </summary>
        /// <param name="consumersToTrack">consumersToTrack to be tracked to prevent wrapping.</param>
        /// <returns>a <see cref="IForceFillProducerBarrier{T}"/> with the above configuration.</returns>
        public IForceFillProducerBarrier<T> CreateForceFillProducerBarrier(params IConsumer[] consumersToTrack)
        {
            return new ForceFillConsumerTrackingProducerBarrier(this, consumersToTrack);
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
            get { return _cursor.VolatileData; }
            private set{ _cursor.VolatileData = value;}
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
        private sealed class ConsumerTrackingConsumerBarrier<TU> : IConsumerBarrier<TU>
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
        /// <see cref="IProducerBarrier{T}"/> that tracks multiple <see cref="IConsumer"/>s when trying to claim
        /// a <see cref="Entry{T}"/> in the <see cref="RingBuffer{T}"/>.
        /// </summary>
        private sealed class ConsumerTrackingProducerBarrier : IProducerBarrier<T>
        {
            private readonly RingBuffer<T> _ringBuffer;
            private readonly IConsumer[] _consumers;

            public ConsumerTrackingProducerBarrier(RingBuffer<T> ringBuffer, params IConsumer[] consumers)
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

                data = _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask].Data;

                return sequence;
            }

            public long NextEntry()
            {
                var sequence = _ringBuffer._claimStrategy.GetAndIncrement();
                EnsureConsumersAreInRange(sequence);
                return sequence;
            }

            // TODO refactor with 2 distinct interfaces

            // reference type, data in the ring buffer as been modified through the reference
            public void Commit(long sequence)
            {
                _ringBuffer._claimStrategy.WaitForCursor(sequence - 1L, _ringBuffer);
                _ringBuffer.Cursor = sequence;
                _ringBuffer._waitStrategy.SignalAll();
            }

            // value type, data needs to be copied back to the ring buffer
            public void Commit(long sequence, T data)
            {
                _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask] = new Entry<T>(sequence, data);
                _ringBuffer._claimStrategy.WaitForCursor(sequence - 1L, _ringBuffer);
                _ringBuffer.Cursor = sequence;
                _ringBuffer._waitStrategy.SignalAll();
            }

            public long Cursor
            {
                get { return _ringBuffer.Cursor; }
            }

            private void EnsureConsumersAreInRange(long sequence)
            {
                while ((sequence - Util.GetMinimumSequence(_consumers)) >= _ringBuffer._entries.Length)
                {
                    Thread.Yield();
                }
            }
        }

        /// <summary>
        /// <see cref="IForceFillProducerBarrier{T}"/> that tracks multiple <see cref="IConsumer"/>s when trying to claim
        ///  a <see cref="Entry{T}"/> in the <see cref="RingBuffer{T}"/>.
        /// </summary>
        private sealed class ForceFillConsumerTrackingProducerBarrier : IForceFillProducerBarrier<T>
        {
            private readonly RingBuffer<T> _ringBuffer;
            private readonly IConsumer[] _consumers;

            public ForceFillConsumerTrackingProducerBarrier(RingBuffer<T> ringBuffer, params IConsumer[] consumers)
            {
                if (consumers.Length == 0)
                {
                    throw new ArgumentException("There must be at least one Consumer to track for preventing ring wrap");
                }

                _ringBuffer = ringBuffer;
                _consumers = consumers;
            }

            public void ClaimEntryValueType(long sequence)
            {
                EnsureConsumersAreInRange(sequence);
            }

            public T ClaimEntry(long sequence)
            {
                EnsureConsumersAreInRange(sequence);

                var entry = _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask];

                return entry.Data;
            }

            //value type
            public void Commit(long sequence, T data)
            {
                _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask] = new Entry<T>(sequence, data);

                _ringBuffer._claimStrategy.SetSequence(sequence + 1L);
                _ringBuffer.Cursor = sequence;
                _ringBuffer._waitStrategy.SignalAll();
            }

            //reference type
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
                while ((sequence - Util.GetMinimumSequence(_consumers)) >= _ringBuffer._entries.Length)
                {
                    Thread.Yield();
                }
            }
        }
    }
}