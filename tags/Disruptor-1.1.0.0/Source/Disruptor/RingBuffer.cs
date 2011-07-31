using System;
using System.Collections.Generic;
using System.Threading;
using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable entries containing the data representing an <see cref="Entry{T}"/> being exchanged between producers and consumers.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class RingBuffer<T> : ISequencable, IConsumerBuilder<T> where T : class 
    {
        private CacheLineStorageLong _cursor = new CacheLineStorageLong(RingBufferConvention.InitialCursorValue);
        private readonly Entry<T>[] _entries;
        private readonly int _ringModMask;
        private readonly IClaimStrategy _claimStrategy;
        private readonly IWaitStrategy _waitStrategy;
        private readonly ConsumerRepository<T> _consumerRepository = new ConsumerRepository<T>();
        private readonly IList<Thread> _threads = new List<Thread>();
        private IBatchConsumer[] _trackedConsumers;
        private readonly int _ringBufferSize;
        private readonly bool _isMultipleProducer;
        private long _lastConsumerMinimum = RingBufferConvention.InitialCursorValue;

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// </summary>
        /// <param name="entryFactory"> entryFactory to create instances of T for filling the RingBuffer</param>
        /// <param name="size">size of the RingBuffer that will be rounded up to the next power of 2</param>
        /// <param name="claimStrategyOption"> threading strategy for producers claiming entries in the ring.</param>
        /// <param name="waitStrategyOption">waiting strategy employed by consumers waiting on entries becoming available.</param>
        public RingBuffer(Func<T> entryFactory, int size, ClaimStrategyOption claimStrategyOption = ClaimStrategyOption.MultipleProducers, WaitStrategyOption waitStrategyOption = WaitStrategyOption.Blocking)
        {
           _ringBufferSize = Util.CeilingNextPowerOfTwo(size);
            _ringModMask = _ringBufferSize - 1;
            _entries = new Entry<T>[_ringBufferSize];
            _claimStrategy = claimStrategyOption.GetInstance();
            _waitStrategy = waitStrategyOption.GetInstance();
            _isMultipleProducer = claimStrategyOption == ClaimStrategyOption.MultipleProducers;

            Fill(entryFactory);
        }

        /// <summary>
        /// The capacity of the RingBuffer to hold entries.
        /// </summary>
        public int Capacity
        {
            get { return _entries.Length; }
        }

        ///<summary>
        /// Claim the next sequence number and a pre-allocated instance of T for a producer on the <see cref="RingBuffer{T}"/>
        ///</summary>
        ///<param name="data">A pre-allocated instance of T to be reused by the producer, to prevent memory allocation. This instance needs to be flushed properly before commiting back to the <see cref="RingBuffer{T}"/></param>
        ///<returns>the claimed sequence.</returns>
        public long NextEntry(out T data)
        {
            var sequence = _claimStrategy.IncrementAndGet();
            EnsureConsumersAreInRange(sequence);

            data = _entries[(int)sequence & _ringModMask].Data;

            return sequence;
        }

        /// <summary>
        ///  Claim the next batch of entries in sequence.
        /// </summary>
        /// <param name="size">size of the batch</param>
        /// <returns>an instance of <see cref="SequenceBatch"/> containing the size and start sequence number of the batch</returns>
        public SequenceBatch NextEntries(int size)
        {
            long sequence = _claimStrategy.IncrementAndGet(size);
            var sequenceBatch = new SequenceBatch(size, sequence);
            EnsureConsumersAreInRange(sequence);

            return sequenceBatch;
        }

        /// <summary>
        /// Commit an entry back to the <see cref="RingBuffer{T}"/> to make it visible to <see cref="IBatchConsumer"/>s
        /// </summary>
        /// <param name="sequence">sequence number to be committed back to the <see cref="RingBuffer{T}"/></param>
        public void Commit(long sequence)
        {
            Commit(sequence, 1L);
        }

        /// <summary>
        /// Commit a batch of entries to the <see cref="RingBuffer{T}"/> to make it visible to <see cref="IBatchConsumer"/>s.
        /// </summary>
        /// <param name="sequenceBatch"></param>
        public void Commit(SequenceBatch sequenceBatch)
        {
            Commit(sequenceBatch.End, sequenceBatch.Size);
        }

        ///<summary>
        /// Get the data for a given sequence from the underlying <see cref="RingBuffer{T}"/>.
        ///</summary>
        ///<param name="sequence">sequence of the entry to get.</param>
        ///<returns>the data for the sequence</returns>
        public T GetEntry(long sequence)
        {
            return _entries[(int)sequence & _ringModMask].Data;
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
        internal Entry<T> this[long sequence]
        {
            get
            {
                return _entries[(int)sequence & _ringModMask];
            }
        }

        internal void SetTrackedConsumer(params IBatchConsumer[] consumersToTrack)
        {
            _trackedConsumers = consumersToTrack;
        }

        ///<summary>
        /// Set up batch handlers to consume events from the ring buffer. These handlers will process events
        /// as soon as they become available, in parallel.
        /// <p/>
        /// <p>This method can be used as the start of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <p/>
        /// <pre><code>dw.consumeWith(A).then(B);</code></pre>
        ///</summary>
        ///<param name="handlers">handlers the batch handlers that will consume events.</param>
        ///<returns>a <see cref="IConsumerGroup{T}"/> that can be used to set up a consumer barrier over the created consumers.</returns>
        public IConsumerGroup<T> ConsumeWith(params IBatchHandler<T>[] handlers)
        {
            return ((IConsumerBuilder<T>) this).CreateConsumers(new IBatchConsumer[0], handlers);
        }

        ///<summary>
        /// Specifies a group of consumers that can then be used to build a barrier for dependent consumers.
        /// For example if the handler <code>A</code> must process events before handler <code>B</code>:
        /// <p/>
        /// <pre><code>dw.after(A).consumeWith(B);</code></pre>
        ///</summary>
        ///<param name="handlers">the batch handlers, previously set up with ConsumeWith,
        /// that will form the barrier for subsequent handlers.</param>
        ///<returns> a <see cref="IConsumerGroup{T}"/> that can be used to setup a consumer barrier over the specified consumers.</returns>
        public IConsumerGroup<T> After(params IBatchHandler<T>[] handlers)
        {
            var selectedConsumers = new IBatchConsumer[handlers.Length];
            for (int i = 0; i < handlers.Length; i++)
            {
                var handler = handlers[i];
                selectedConsumers[i] = _consumerRepository.GetConsumerFor(handler);
                if (selectedConsumers[i] == null)
                {
                    throw new InvalidOperationException("Batch handlers must be consuming from the ring buffer before they can be used in a barrier condition.");
                }
            }
            return new ConsumerGroup<T>(this, selectedConsumers);
        }

        /// <summary>
        ///  Create a <see cref="IConsumerBarrier{T}"/> that gates on the RingBuffer and a list of <see cref="IBatchConsumer"/>s
        /// </summary>
        /// <param name="consumersToTrack">consumersToTrack this barrier will track</param>
        /// <returns>the barrier gated as required</returns>
        internal IConsumerBarrier<T> CreateConsumerBarrier(params IBatchConsumer[] consumersToTrack)
        {
            return new ConsumerBarrier<T>(this, consumersToTrack);
        }

        ///<summary>
        /// Calls <see cref="IBatchConsumer.Halt"/> on all the consumers
        ///</summary>
        public void Halt()
        {
            foreach (var consumerInfo in _consumerRepository.Consumers)
            {
                consumerInfo.BatchConsumer.Halt();
            }
            foreach (var thread in _threads)
            {
                thread.Join();
            }
        }

        /// <summary>
        /// Start all consumer threads
        /// </summary>
        public void StartConsumers()
        {
            RetrieveConsumersToTrack();

            foreach (var consumerInfo in _consumerRepository.Consumers)
            {
                var thread = new Thread(consumerInfo.BatchConsumer.Run) { IsBackground = true };
                _threads.Add(thread);
                thread.Start();
            }

            //wait all BatchConsumers are properly started
            foreach (var consumerInfo in _consumerRepository.Consumers)
            {
                while (!consumerInfo.BatchConsumer.Running)
                {
                    // busy spin
                }
            }
        }

        private void RetrieveConsumersToTrack()
        {
            var lastConsumersInChain = _consumerRepository.LastConsumersInChain;
            var period = _entries.Length / 2;
            foreach (var batchConsumer in lastConsumersInChain)
            {
                batchConsumer.DelaySequenceWrite(period);
            }
            _trackedConsumers = lastConsumersInChain;
        }

        private void EnsureConsumersAreInRange(long sequence)
        {
            var wrapPoint = sequence - _ringBufferSize;

            while (wrapPoint > _lastConsumerMinimum && wrapPoint > (_lastConsumerMinimum = _trackedConsumers.GetMinimumSequence()))
            {
                Thread.Yield();
            }
        }

        private void Commit(long sequence, long batchSize)
        {
            if (_isMultipleProducer)
            {
                long expectedSequence = sequence - batchSize;
                
                var spin = new SpinWait();
                while (expectedSequence != Cursor)
                {
                    spin.SpinOnce();
                }
            }

            Cursor = sequence; // volatile write
            _waitStrategy.SignalAll();
        }

        ConsumerGroup<T> IConsumerBuilder<T>.CreateConsumers(IBatchConsumer[] barrierConsumers, IBatchHandler<T>[] batchHandlers)
        {
            if(_trackedConsumers != null)
            {
                throw new InvalidOperationException("Producer Barrier must be initialised after all consumer barriers.");
            }

            var createdConsumers = new IBatchConsumer[batchHandlers.Length];
            for (int i = 0; i < batchHandlers.Length; i++)
            {
                var batchHandler = batchHandlers[i];
                var barrier = new ConsumerBarrier<T>(this, barrierConsumers);
                var batchConsumer = new BatchConsumer<T>(barrier, batchHandler);

                _consumerRepository.Add(batchConsumer, batchHandler);
                createdConsumers[i] = batchConsumer;
            }

            _consumerRepository.UnmarkConsumersAsEndOfChain(barrierConsumers);
            return new ConsumerGroup<T>(this, createdConsumers);
        }

        private void Fill(Func<T> entryFactory)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                var data = entryFactory();
                _entries[i] = new Entry<T>(-1, data);
            }
        }

        /// <summary>
        /// ConsumerBarrier handed out for gating consumers of the RingBuffer and dependent <see cref="IBatchConsumer"/>(s)
        /// </summary>
        /// <typeparam name="TU"></typeparam>
        private sealed class ConsumerBarrier<TU> : IConsumerBarrier<TU> where TU : class
        {
            private readonly IBatchConsumer[] _consumers;
            private volatile bool _alerted;
            private readonly RingBuffer<TU> _ringBuffer;
            private readonly Entry<TU>[] _entries;
            private readonly int _ringModMask;
            private readonly IWaitStrategy _waitStrategy;

            public ConsumerBarrier(RingBuffer<TU> ringBuffer, params IBatchConsumer[] consumers)
            {
                _ringBuffer = ringBuffer;
                _consumers = consumers;
                _waitStrategy = _ringBuffer._waitStrategy;
                _ringModMask = _ringBuffer._ringModMask;
                _entries = _ringBuffer._entries;
            }

            public TU GetEntry(long sequence)
            {
                return _entries[(int)sequence & _ringModMask].Data;
            }

            public WaitForResult WaitFor(long sequence)
            {
                return _waitStrategy.WaitFor(_consumers, _ringBuffer, this, sequence);
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
                _waitStrategy.SignalAll();
            }

            public void ClearAlert()
            {
                _alerted = false;
            }
        }
    }
}