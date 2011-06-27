using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Convenience class for handling the batching semantics of consuming entries from a <see cref="RingBuffer{T}"/>
    /// and delegating the available <see cref="IEntry"/>s to a <see cref="IBatchHandler{T}"/>.
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class BatchConsumer<T>:IConsumer
        where T:IEntry
    {
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
        public long _p1, _p2, _p3, _p4, _p5, _p6, _p7; // cache line padding
        private volatile bool _running = true;
        public long _p8, _p9, _p10, _p11, _p12, _p13, _p14; // cache line padding
        private long _sequence = -1L;
        public long _p15, _p16, _p17, _p18, _p19; // cache line padding
// ReSharper restore InconsistentNaming
// ReSharper restore UnusedMember.Global

        private readonly IConsumerBarrier<T> _consumerBarrier;
        private readonly IBatchHandler<T> _handler;
        private readonly bool _noSequenceTracker;
        private IExceptionHandler _exceptionHandler = new FatalExceptionHandler();

        /// <summary>
        /// Construct a batch consumer that will automatically track the progress by updating its sequence when
        /// the <see cref="IBatchHandler{T}.OnAvailable"/> method returns.
        /// </summary>
        /// <param name="consumerBarrier">consumerBarrier on which it is waiting.</param>
        /// <param name="handler">handler is the delegate to which <see cref="IEntry"/>s are dispatched.</param>
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
        /// Set a new <see cref="IExceptionHandler"/> for handling exceptions propagated out of the <see cref="BatchConsumer{T}"/>
        /// </summary>
        /// <param name="exceptionHandler">exceptionHandler to replace the existing exceptionHandler.</param>
        public void SetExceptionHandler(IExceptionHandler exceptionHandler)
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
            _running = true;
            var entry = default(T);

            while (_running)
            {
                try
                {
                    
                    var nextSequence = Sequence + 1;
                    var availableSeq = _consumerBarrier.WaitFor(nextSequence);

                    for (var i = nextSequence; i <= availableSeq; i++)
                    {
                        entry = _consumerBarrier.GetEntry(i);
                        _handler.OnAvailable(entry);

                        if (_noSequenceTracker)
                        {
                            Sequence = entry.Sequence;
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
                    _exceptionHandler.Handle(ex, entry);
                    if (_noSequenceTracker)
                    {
                        Sequence = entry.Sequence;
                    }
                }
            }

            _handler.OnCompletion();
        }

        public long Sequence
        {
            get { return Thread.VolatileRead(ref _sequence); }
            private set { Thread.VolatileWrite(ref _sequence, value);}
        }

        public void Halt()
        {
            _running = false;
            _consumerBarrier.Alert();
        }

        /// <summary>
        /// Used by the <see cref="IBatchHandler{T}"/> to signal when it has completed consuming a given sequence.
        /// </summary>
        public class SequenceTrackerCallback
        {
            private readonly BatchConsumer<T> _batchConsumer;

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