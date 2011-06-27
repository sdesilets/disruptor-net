using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable entries containing the data representing an <see cref="IEntry"/> being exchanged between producers and consumers.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class RingBuffer<T> where T : IEntry
    {
        // Set to -1 as sequence starting point 
        public const long InitialCursorValue = -1L;

        // ReSharper disable InconsistentNaming
        public long p1, p2, p3, p4, p5, p6, p7; // cache line padding INITCURSOR_VALUE;
        private long _cursor = InitialCursorValue;
        public long p8, p9, p10, p11, p12, p14; // cache line padding
        // ReSharper restore InconsistentNaming

        private readonly T[] _entries;
        private readonly int _ringModMask;

        private readonly IClaimStrategy _claimStrategy;
        private readonly IWaitStrategy _waitStrategy;

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// </summary>
        /// <param name="entryFactory"> entryFactory to create<see cref="IEntry"/>s for filling the RingBuffer</param>
        /// <param name="size">size of the RingBuffer that will be rounded up to the next power of 2</param>
        /// <param name="claimStrategyOption"> threading strategy for producers claiming <see cref="IEntry"/>s in the ring.</param>
        /// <param name="waitStrategyOption">waiting strategy employed by consumers waiting on <see cref="IEntry"/>s becoming available.</param>
        public RingBuffer(IEntryFactory<T> entryFactory, int size, ClaimStrategyFactory.ClaimStrategyOption claimStrategyOption = ClaimStrategyFactory.ClaimStrategyOption.Multithreaded, WaitStrategyFactory.WaitStrategyOption waitStrategyOption = WaitStrategyFactory.WaitStrategyOption.Blocking)
        {
            var sizeAsPowerOfTwo = Util.CeilingNextPowerOfTwo(size);
            _ringModMask = sizeAsPowerOfTwo - 1;
            _entries = new T[sizeAsPowerOfTwo];

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
            get { return Thread.VolatileRead(ref _cursor); }
            private set{Thread.VolatileWrite(ref _cursor, value);}
        }

        /// <summary>
        /// Get the <see cref="IEntry"/> for a given sequence in the RingBuffer.
        /// </summary>
        /// <param name="sequence">sequence for the <see cref="IEntry"/></param>
        /// <returns><see cref="IEntry"/> for the sequence</returns>
        public T GetEntry(long sequence)
        {
            return _entries[(int)sequence & _ringModMask];
        }

        private void Fill(IEntryFactory<T> entryEntryFactory)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                _entries[i] = entryEntryFactory.Create();
            }
        }

        /// <summary>
        /// ConsumerBarrier handed out for gating consumers of the RingBuffer and dependent <see cref="IConsumer"/>(s)
        /// </summary>
        /// <typeparam name="TU"></typeparam>
        private sealed class ConsumerTrackingConsumerBarrier<TU> : IConsumerBarrier<TU> where TU : IEntry
        {
            // ReSharper disable InconsistentNaming
#pragma warning disable 169
            public long p1, p2, p3, p4, p5, p6, p7; // cache line padding
            private readonly IConsumer[] _consumers;
            private volatile bool _alerted;
            public long p8, p9, p10, p11, p12, p13, p14; // cache line padding
            private readonly RingBuffer<TU> _ringBuffer;
            // ReSharper restore InconsistentNaming
#pragma warning restore 169

            public ConsumerTrackingConsumerBarrier(RingBuffer<TU> ringBuffer, params IConsumer[] consumers)
            {
                _ringBuffer = ringBuffer;
                _consumers = consumers;
            }

            public TU GetEntry(long sequence)
            {
                return _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask];
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
        /// a <see cref="IEntry"/> in the <see cref="RingBuffer{T}"/>.
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

            public T NextEntry()
            {
                var sequence = _ringBuffer._claimStrategy.GetAndIncrement();
                EnsureConsumersAreInRange(sequence);

                var entry = _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask];
                entry.Sequence = sequence;

                return entry;
            }

            public void Commit(T entry)
            {
                var sequence = entry.Sequence;
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
        ///  a <see cref="IEntry"/> in the <see cref="RingBuffer{T}"/>.
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

            public T ClaimEntry(long sequence)
            {
                EnsureConsumersAreInRange(sequence);

                var entry = _ringBuffer._entries[(int)sequence & _ringBuffer._ringModMask];
                entry.Sequence = sequence;

                return entry;
            }

            public void Commit(T entry)
            {
                var sequence = entry.Sequence;
                _ringBuffer._claimStrategy.SetSequence(sequence + 1L);
                _ringBuffer.Cursor = sequence;
                _ringBuffer._waitStrategy.SignalAll();
            }

            public long GetCursor()
            {
                return _ringBuffer.Cursor;
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