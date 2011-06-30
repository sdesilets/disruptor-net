using System;
using Disruptor.MemoryLayout;

namespace Disruptor
{
    /// <summary>
    /// Convenience class for handling the batching semantics of consuming entries from a <see cref="RingBuffer{T}"/>
    /// and delegating the available <see cref="Entry{T}"/>s to a <see cref="IBatchHandler{T}"/>.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class BatchConsumer<T> : IConsumer
    {
        private CacheLineStorageBool _running = new CacheLineStorageBool(true);
        private CacheLineStorageLong _sequence = new CacheLineStorageLong(-1L);
        private readonly IConsumerBarrier<T> _consumerBarrier;
        private readonly IBatchHandler<T> _handler;
        private readonly bool _noSequenceTracker;
        private IExceptionHandler<T> _exceptionHandler = new FatalExceptionHandler<T>();

        /// <summary>
        /// Construct a batch consumer that will automatically track the progress by updating its sequence when
        /// the <see cref="IBatchHandler{T}.OnAvailable"/> method returns.
        /// </summary>
        /// <param name="consumerBarrier">consumerBarrier on which it is waiting.</param>
        /// <param name="handler">handler is the delegate to which <see cref="Entry{T}"/>s are dispatched.</param>
        public BatchConsumer(IConsumerBarrier<T> consumerBarrier, IBatchHandler<T> handler)
        {
            _consumerBarrier = consumerBarrier;
            _handler = handler;
            _noSequenceTracker = true;
        }

        /// <summary>
        /// Construct a batch consumer that will rely on the <see cref="ISequenceTrackingHandler{T}"/>
        /// to callback via the <see cref="BatchConsumer{T}.SequenceTrackerCallback"/> when it has completed with a sequence.
        /// </summary>
        /// <param name="consumerBarrier"></param>
        /// <param name="entryHandler"></param>
        public BatchConsumer(IConsumerBarrier<T> consumerBarrier, ISequenceTrackingHandler<T> entryHandler)
        {
            _consumerBarrier = consumerBarrier;
            _handler = entryHandler;
            _noSequenceTracker = false;

            entryHandler.SetSequenceTrackerCallback(new SequenceTrackerCallback(this));
        }

        /// <summary>
        /// Set a new <see cref="IExceptionHandler{T}"/> for handling exceptions propagated out of the <see cref="BatchConsumer{T}"/>
        /// </summary>
        /// <param name="exceptionHandler">exceptionHandler to replace the existing exceptionHandler.</param>
        public void SetExceptionHandler(IExceptionHandler<T> exceptionHandler)
        {
            if (exceptionHandler == null)
            {
                throw new ArgumentNullException("exceptionHandler");
            }

            _exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Get the <see cref="IConsumerBarrier{T}"/> the <see cref="IConsumer"/> is waiting on.
        /// </summary>
        public IConsumerBarrier<T> ConsumerBarrier
        {
            get { return _consumerBarrier; }
        }


        /// <summary>
        /// It is ok to have another thread rerun this method after a halt().
        /// </summary>
        public void Run()
        {
            _running.VolatileData = true;
            var data = default(T);
            var i = 0L;

            while (_running.VolatileData)
            {
                try
                {
                    
                    var nextSequence = Sequence + 1;
                    var availableSeq = _consumerBarrier.WaitFor(nextSequence);

                    for (i = nextSequence; i <= availableSeq; i++)
                    {
                        data = _consumerBarrier.GetEntry(i);
                        _handler.OnAvailable(i, data);

                        if (_noSequenceTracker)
                        {
                            Sequence = i;
                        }
                    }

                    _handler.OnEndOfBatch();
                }
                catch (AlertException)
                {
                    // Wake up from blocking wait and check if we should continue to run
                }
                catch (Exception ex)
                {
                    _exceptionHandler.Handle(ex, new Entry<T>(i, data));
                    if (_noSequenceTracker)
                    {
                        Sequence = i;
                    }
                }
            }

            _handler.OnCompletion();
        }

        /// <summary>
        /// Get the sequence up to which this Consumer has consumed <see cref="Entry{T}"/>s
        /// Return the sequence of the last consumed <see cref="Entry{T}"/>
        /// </summary>
        public long Sequence
        {
            get { return _sequence.VolatileData; }
            private set { _sequence.VolatileData = value;}
        }

        /// <summary>
        /// Signal that this Consumer should stop when it has finished consuming at the next clean break.
        /// It will call <see cref="IConsumerBarrier{T}.Alert"/> to notify the thread to check status.
        /// </summary>
        public void Halt()
        {
            _running.VolatileData = false;
            _consumerBarrier.Alert();
        }

        /// <summary>
        /// Used by the <see cref="IBatchHandler{T}"/> to signal when it has completed consuming a given sequence.
        /// </summary>
        public class SequenceTrackerCallback
        {
            private readonly BatchConsumer<T> _batchConsumer;

            ///<summary>
            /// 
            ///</summary>
            ///<param name="batchConsumer"></param>
            public SequenceTrackerCallback(BatchConsumer<T> batchConsumer)
            {
                _batchConsumer = batchConsumer;
            }

            /// <summary>
            /// Notify that the handler has consumed up to a given sequence.
            /// </summary>
            /// <param name="sequence">sequence that has been consumed.</param>
            public void OnComplete(long sequence)
            {
                _batchConsumer.Sequence = sequence;
            }
        }
    }
}