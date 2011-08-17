using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Factory used by the <see cref="RingBuffer{T}"/> to instantiate the selected <see cref="IWaitStrategy"/>.
    /// </summary>
    public static class WaitStrategyExtensions
    {
        /// <summary>
        /// Used by the <see cref="RingBuffer{T}"/> as a polymorphic constructor.
        /// </summary>
        /// <param name="option">Strategy type.</param>
        /// <returns>a new instance of the WaitStrategy</returns>
        public static IWaitStrategy GetInstance(this WaitStrategyOption option)
        {
            switch (option)
            {
                case WaitStrategyOption.Blocking:
                    return new BlockingStrategy();
                case WaitStrategyOption.Yielding:
                    return new YieldingStrategy();
                case WaitStrategyOption.BusySpin:
                    return new BusySpinStrategy();
                    //TODO implement a strategy based on SpinWait
                default:
                    throw new ArgumentOutOfRangeException("option");
            }
        }

        /// <summary>
        /// Blocking strategy that uses a lock and condition variable for <see cref="IEventProcessor"/>s waiting on a barrier.
        /// This strategy should be used when performance and low-latency are not as important as CPU resource.
        /// </summary>
        private sealed class BlockingStrategy : IWaitStrategy
        {
            private readonly object _gate = new object();

            public WaitForResult WaitFor(Sequence[] dependents, Sequence cursor, IDependencyBarrier barrier, long sequence)
            {
                var availableSequence = cursor.Value; // volatile read
                if (availableSequence < sequence)
                {
                    lock (_gate)
                    {
                        while ((availableSequence = cursor.Value) < sequence) // volatile read
                        {
                            if (barrier.IsAlerted)
                            {
                                return WaitForResult.AlertedResult;
                            }

                            Monitor.Wait(_gate);
                        }
                    }
                }

                if (0 != dependents.Length)
                {
                    while ((availableSequence = dependents.GetMinimumSequence()) < sequence)
                    {
                        if (barrier.IsAlerted)
                        {
                            return WaitForResult.AlertedResult;
                        }
                    }
                }

                return new WaitForResult(availableSequence, false);
            }

            public void SignalAll()
            {
                lock(_gate)
                {
                    Monitor.PulseAll(_gate);
                }
            }
        }

        /// <summary>
        /// Yielding strategy that uses a Thread.yield() for <see cref="IEventProcessor"/>s waiting on a barrier.
        /// This strategy is a good compromise between performance and CPU resource.
        /// </summary>
        private sealed class YieldingStrategy:IWaitStrategy
        {
            private const int SpinTries = 100;

            public WaitForResult WaitFor(Sequence[] dependents, Sequence cursor, IDependencyBarrier barrier, long sequence)
            {
                long availableSequence;

                var counter = SpinTries;
                if (0 == dependents.Length)
                {
                    while ((availableSequence = cursor.Value) < sequence) // volatile read
                    {
                        if (barrier.IsAlerted)
                        {
                            return WaitForResult.AlertedResult;
                        }

                        if(0 == --counter)
                        {
                            Thread.Yield();
                            counter = SpinTries;
                        }
                    }
                }
                else
                {
                    while ((availableSequence = dependents.GetMinimumSequence()) < sequence)
                    {
                        if (barrier.IsAlerted)
                        {
                            return WaitForResult.AlertedResult;
                        }

                        if (0 == --counter)
                        {
                            Thread.Yield();
                            counter = SpinTries;
                        }
                    }
                }

                return new WaitForResult(availableSequence, false);
            }

            public void SignalAll()
            {
            }
        }

        /// <summary>
        /// Busy Spin strategy that uses a busy spin loop for <see cref="IEventProcessor"/>s waiting on a barrier.
        /// This strategy will use CPU resource to avoid syscalls which can introduce latency jitter.  It is best
        /// used when threads can be bound to specific CPU cores.
        /// </summary>
        private sealed class BusySpinStrategy:IWaitStrategy
        {
            public WaitForResult WaitFor(Sequence[] dependents, Sequence cursor, IDependencyBarrier barrier, long sequence)
            {
                long availableSequence;

                if (0 == dependents.Length)
                {
                    while ((availableSequence = cursor.Value) < sequence) // volatile read
                    {
                        if (barrier.IsAlerted)
                        {
                            return WaitForResult.AlertedResult;
                        }
                    }
                }
                else
                {
                    while ((availableSequence = dependents.GetMinimumSequence()) < sequence)
                    {
                        if (barrier.IsAlerted)
                        {
                            return WaitForResult.AlertedResult;
                        }
                    }
                }

                return new WaitForResult(availableSequence, false);
            }

            public void SignalAll()
            {
            }
        }
    }
}