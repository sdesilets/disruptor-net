using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable events containing the data representing an <see cref="Event{T}"/> being exchanged between producers and eventProcessors.
    /// </summary>
    /// <typeparam name="T">Event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class RingBuffer<T> : IEventProcessorBuilder<T> where T : class 
    {
        private readonly Sequence _cursor = new Sequence(RingBufferConvention.InitialCursorValue);
        private readonly int _ringModMask;
        private readonly T[] _events;

        private Sequence[] _processorSequencesToTrack;

        private readonly IClaimStrategy _claimStrategy;
        private readonly IWaitStrategy _waitStrategy;

        private readonly EventProcessorRepository<T> _eventProcessorRepository = new EventProcessorRepository<T>();
        private readonly IList<Thread> _threads = new List<Thread>();
        private readonly int _ringBufferSize;

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// </summary>
        /// <param name="eventFactory">Factory to create instances of T for filling the RingBuffer</param>
        /// <param name="size">size of the RingBuffer that will be rounded up to the next power of 2</param>
        /// <param name="claimStrategyOption"> threading strategy for producers claiming events in the ring.</param>
        /// <param name="waitStrategyOption">waiting strategy employed by <see cref="EventProcessor{T}"/> waiting on events becoming available.</param>
        public RingBuffer(Func<T> eventFactory, int size, ClaimStrategyOption claimStrategyOption = ClaimStrategyOption.MultipleProducers, WaitStrategyOption waitStrategyOption = WaitStrategyOption.Blocking)
        {
            _ringBufferSize = Util.CeilingNextPowerOfTwo(size);
            _ringModMask = _ringBufferSize - 1;
            _events = new T[_ringBufferSize];
            _claimStrategy = claimStrategyOption.GetInstance(_ringBufferSize);
            _waitStrategy = waitStrategyOption.GetInstance();

            Fill(eventFactory);
        }

        /// <summary>
        /// The capacity of the RingBuffer to hold events.
        /// </summary>
        public int Capacity
        {
            get { return _events.Length; }
        }

        ///<summary>
        /// Claim the next sequence number and a pre-allocated instance of T for a producer on the <see cref="RingBuffer{T}"/>
        ///</summary>
        ///<returns>the claimed event containing a pre-allocated instance of T to be reused by the producer, to prevent memory allocation. This instance needs to be flushed properly before commiting back to the <see cref="RingBuffer{T}"/></returns>
        public Event<T> NextEvent()
        {
            var sequence = _claimStrategy.IncrementAndGet();
            _claimStrategy.EnsureProcessorsAreInRange(sequence, _processorSequencesToTrack);

            var evt = new Event<T>(sequence, _events[(int)sequence & _ringModMask]);

            return evt;
        }

        /// <summary>
        ///  Claim the next batch of events in sequence.
        /// </summary>
        /// <param name="size">size of the batch</param>
        /// <returns>an instance of <see cref="SequenceBatch"/> containing the size and start sequence number of the batch</returns>
        public SequenceBatch NextEvents(int size)
        {
            long sequence = _claimStrategy.IncrementAndGet(size);
            var sequenceBatch = new SequenceBatch(size, sequence);
            _claimStrategy.EnsureProcessorsAreInRange(sequence, _processorSequencesToTrack);

            return sequenceBatch;
        }

        /// <summary>
        /// Publish an event back to the <see cref="RingBuffer{T}"/> to make it visible to <see cref="IEventProcessor"/>s
        /// </summary>
        /// <param name="publishedEvent">event to be committed back to the <see cref="RingBuffer{T}"/></param>
        public void Publish(Event<T> publishedEvent)
        {
            Publish(publishedEvent.Sequence, 1L);
        }

        /// <summary>
        /// Publish a batch of ev events the <see cref="RingBuffer{T}"/> to make it visible to <see cref="IEventProcessor"/>s.
        /// </summary>
        /// <param name="sequenceBatch"></param>
        public void Publish(SequenceBatch sequenceBatch)
        {
            Publish(sequenceBatch.End, sequenceBatch.Size);
        }

        /// <summary>
        /// Get the current sequence that producers have committed to the RingBuffer.
        /// </summary>
        public long Cursor
        {
            get { return _cursor.Value; }
        }

        ///<summary>
        /// Get the event for a given sequence in the RingBuffer.
        ///</summary>
        ///<param name="sequence">sequence for the <see cref="Event{T}"/></param>
        public T this[long sequence]
        {
            get { return _events[(int)sequence & _ringModMask]; }
        }

        internal void SetTrackedEventProcessors(params IEventProcessor[] eventProcessorsToTrack)
        {
            _processorSequencesToTrack = eventProcessorsToTrack.Select(ep => ep.Sequence).ToArray();
        }

        ///<summary>
        /// Set up <see cref="IEventHandler{T}"/> to process events from the ring buffer. These handlers will process events
        /// as soon as they become available, in parallel.
        /// <p/>
        /// <p>This method can be used as the start of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <p/>
        /// <pre><code>dw.HandleEventsWith(A).Then(B);</code></pre>
        ///</summary>
        ///<param name="eventHandlers">the <see cref="IEventHandler{T}"/>s that will process events.</param>
        ///<returns>a <see cref="IEventProcessorsGroup{T}"/> that can be used to set up a <see cref="IDependencyBarrier"/> over the created <see cref="IEventProcessor"/>.</returns>
        public IEventProcessorsGroup<T> HandleEventsWith(params IEventHandler<T>[] eventHandlers)
        {
            return ((IEventProcessorBuilder<T>) this).CreateEventProcessors(new IEventProcessor[0], eventHandlers);
        }

        ///<summary>
        /// Specifies a group of <see cref="IEventHandler{T}"/> that can then be used to build a <see cref="IDependencyBarrier"/> for dependent <see cref="IEventProcessor"/>s.
        /// For example if the handler <code>A</code> must process events before handler <code>B</code>:
        /// <p/>
        /// <pre><code>dw.After(A).HandleEventsWith(B);</code></pre>
        ///</summary>
        ///<param name="eventHandlers">the <see cref="IEventHandler{T}"/>s, previously set up with HandleEventsWith,
        /// that will form the barrier for subsequent handlers.</param>
        ///<returns> a <see cref="IEventProcessorsGroup{T}"/> that can be used to setup a <see cref="IDependencyBarrier"/> over the specified <see cref="IEventProcessor"/>.</returns>
        public IEventProcessorsGroup<T> After(params IEventHandler<T>[] eventHandlers)
        {
            var eventProcessors = new IEventProcessor[eventHandlers.Length];
            for (int i = 0; i < eventHandlers.Length; i++)
            {
                var handler = eventHandlers[i];
                eventProcessors[i] = _eventProcessorRepository.GetEventProcessorFor(handler);
                if (eventProcessors[i] == null)
                {
                    throw new InvalidOperationException("Event handlers must be consuming from the ring buffer before they can be used in a barrier condition.");
                }
            }
            return new EventProcessorsGroup<T>(this, eventProcessors);
        }

        /// <summary>
        ///  Create a <see cref="IDependencyBarrier"/> that gates on the RingBuffer and a list of <see cref="IEventProcessor"/>s
        /// </summary>
        /// <param name="eventProcessorsToTrack">eventProcessorsToTrack this barrier will track</param>
        /// <returns>the barrier gated as required</returns>
        internal IDependencyBarrier CreateDependencyBarrier(params IEventProcessor[] eventProcessorsToTrack)
        {
            var processorSequences = eventProcessorsToTrack.Select(ep => ep.Sequence).ToArray();

            return new DependencyBarrier(_waitStrategy, _cursor, processorSequences);
        }

        ///<summary>
        /// Calls <see cref="IEventProcessor.Halt"/> on all the eventProcessors
        ///</summary>
        public void Halt()
        {
            foreach (var eventProcessorInfo in _eventProcessorRepository.EventProcessors)
            {
                eventProcessorInfo.EventProcessor.Halt();
            }
            foreach (var thread in _threads)
            {
                thread.Join();
            }
        }

        /// <summary>
        /// Start all <see cref="IEventProcessor"/> threads
        /// </summary>
        public void StartProcessors()
        {
            RetrieveEventProcessorsToTrack();

            foreach (var eventProcessorInfo in _eventProcessorRepository.EventProcessors)
            {
                var thread = new Thread(eventProcessorInfo.EventProcessor.Run) { IsBackground = true };
                _threads.Add(thread);
                thread.Start();
            }

            //wait all event processors to be started
            foreach (var eventProcessorInfo in _eventProcessorRepository.EventProcessors)
            {
                while (!eventProcessorInfo.EventProcessor.Running)
                {
                    // busy spin
                }
            }
        }

        private void RetrieveEventProcessorsToTrack()
        {
            var lastEventProcessorsInChain = _eventProcessorRepository.LastEventProcessorsInChain;
            var period = _events.Length / 2;
            foreach (var eventProcessor in lastEventProcessorsInChain)
            {
                eventProcessor.DelaySequenceWrite(period);
            }
            SetTrackedEventProcessors(lastEventProcessorsInChain);
        }

        private void Publish(long sequence, long batchSize)
        {
            _claimStrategy.SerialisePublishing(_cursor, sequence, batchSize);
            _cursor.Value = sequence; // volatile write
            _waitStrategy.SignalAll();
        }

        EventProcessorsGroup<T> IEventProcessorBuilder<T>.CreateEventProcessors(IEnumerable<IEventProcessor> barrierEventProcessors, IEventHandler<T>[] eventHandlers)
        {
            if(_processorSequencesToTrack != null)
            {
                throw new InvalidOperationException("Producer Barrier must be initialised after all event processor barriers.");
            }

            var createdEventProcessors = new IEventProcessor[eventHandlers.Length];
            for (int i = 0; i < eventHandlers.Length; i++)
            {
                var batchHandler = eventHandlers[i];
                var barrier = new DependencyBarrier(_waitStrategy, _cursor, barrierEventProcessors.Select(ep=>ep.Sequence).ToArray());
                var eventProcessor = new EventProcessor<T>(this, barrier, batchHandler);

                _eventProcessorRepository.Add(eventProcessor, batchHandler);
                createdEventProcessors[i] = eventProcessor;
            }

            _eventProcessorRepository.UnmarkEventProcessorsAsEndOfChain(barrierEventProcessors);
            return new EventProcessorsGroup<T>(this, createdEventProcessors);
        }

        private void Fill(Func<T> eventFactory)
        {
            for (var i = 0; i < _events.Length; i++)
            {
                _events[i] = eventFactory();
            }
        }
    }
}