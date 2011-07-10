using System;

namespace Disruptor
{
    /// <summary>
    /// Callback handler for uncaught exceptions in the <see cref="Entry{T}"/> processing cycle of the <see cref="BatchConsumer{T}"/>
    /// </summary>
    public interface IExceptionHandler<T>
    {
        /// <summary>
        /// Strategy for handling uncaught exceptions when processing an <see cref="Entry{T}"/>.
        /// If the strategy wishes to suspend further processing by the <see cref="BatchConsumer{T}"/>
        /// then is should throw a <see cref="DisruptorFatalException"/>
        /// </summary>
        /// <param name="ex">the exception that propagated from the <see cref="IBatchHandler{T}"/></param>
        /// <param name="currentEntry">currentEntry being processed when the exception occurred.</param>
        void Handle(Exception ex, Entry<T> currentEntry);
    }
}