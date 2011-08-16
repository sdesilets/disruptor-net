using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Convenience class for handling the batching semantics of consuming events from a <see cref="RingBuffer{T}"/>
    /// and delegating the available <see cref="Event{T}"/>s to a <see cref="IEventHandler{T}"/>.
    /// 
    /// If the <see cref="EventProcessor{T}"/> also implements <see cref="ILifecycleAware"/> it will be notified just after the thread
    /// is started and just before the thread is shutdown.
    /// </summary>
    /// <typeparam name="T">Event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    internal sealed class EventProcessor<T> : IEventProcessor
    {
        private readonly IDependencyBarrier<T> _dependencyBarrier;
        private readonly IEventHandler<T> _eventHandler;

        private CacheLineStorageBool _running = new CacheLineStorageBool(true);
        private CacheLineStorageLong _sequence = new CacheLineStorageLong(RingBufferConvention.InitialCursorValue);
        private bool _delaySequenceWrite;
        private int _sequenceUpdatePeriod;
        private int _nextSequencePublish = 1;

        /// <summary>
        /// Construct a <see cref="EventProcessor{T}"/> that will automatically track the progress by updating its sequence when
        /// the <see cref="IEventHandler{T}.OnAvailable"/> method returns.
        /// </summary>
        /// <param name="dependencyBarrier">dependencyBarrier on which it is waiting.</param>
        /// <param name="eventHandler">eventHandler is the delegate to which <see cref="Event{T}"/>s are dispatched.</param>
        public EventProcessor(IDependencyBarrier<T> dependencyBarrier, IEventHandler<T> eventHandler)
        {
            _dependencyBarrier = dependencyBarrier;
            _eventHandler = eventHandler;
        }

        /// <summary>
        /// Throttle the sequence publication to other threads
        /// Can only be applied to the last eventProcessors of a chain (the one tracked by the producer barrier)
        /// </summary>
        /// <param name="period">Sequence will be published every 'period' events</param>
        public void DelaySequenceWrite(int period)
        {
            _delaySequenceWrite = true;
            _sequenceUpdatePeriod = period;
            _nextSequencePublish = period;
        }

        /// <summary>
        /// Get the <see cref="IDependencyBarrier{T}"/> the <see cref="IEventProcessor"/> is waiting on.
        /// </summary>
        public IDependencyBarrier<T> DependencyBarrier
        {
            get { return _dependencyBarrier; }
        }

        /// <summary>
        /// Return true if the instance is started, false otherwise
        /// </summary>
        public bool Running
        {
            get { return _running.Data; }
        }

        /// <summary>
        /// It is ok to have another thread rerun this method after a halt().
        /// </summary>
        public void Run()
        {
            _running.Data = true;
            
            OnStart();

            var nextSequence = Sequence + 1; 

            while (_running.Data)
            {
                var waitForResult = _dependencyBarrier.WaitFor(nextSequence);
                if (!waitForResult.IsAlerted)
                {
                    var availableSequence = waitForResult.AvailableSequence;
                    while (nextSequence <= availableSequence)
                    {
                        T data = _dependencyBarrier.GetEvent(nextSequence);
                        _eventHandler.OnAvailable(nextSequence, data);
                        
                        nextSequence++;
                    }
                    _eventHandler.OnEndOfBatch();

                    if(_delaySequenceWrite)
                    {
                        if(nextSequence > _nextSequencePublish)
                        {
                            Sequence = nextSequence - 1; // volatile write
                            _nextSequencePublish += _sequenceUpdatePeriod;
                        }
                    }
                    else
                    {
                        Sequence = nextSequence - 1; // volatile write
                    }
                }
            }

            OnStop();
        }

        private void OnStop()
        {
            var lifecycleAware = _eventHandler as ILifecycleAware;
            if (lifecycleAware != null)
            {
                lifecycleAware.OnStop();
            }
        }

        private void OnStart()
        {
            var lifecycleAware = _eventHandler as ILifecycleAware;
            if(lifecycleAware != null)
            {
                lifecycleAware.OnStart();
            }
        }

        /// <summary>
        /// Get the sequence up to which this <see cref="IEventProcessor"/> has consumed <see cref="Event{T}"/>s
        /// Return the sequence of the last consumed <see cref="Event{T}"/>
        /// </summary>
        public long Sequence
        {
            get { return _sequence.Data; }
            private set { _sequence.Data = value;}
        }

        /// <summary>
        /// Signal that this <see cref="IEventProcessor"/> should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="IDependencyBarrier{T}.Alert"/> to notify the thread to check status.
        /// </summary>
        public void Halt()
        {
            _running.Data = false;
            _dependencyBarrier.Alert();
        }
    }
}