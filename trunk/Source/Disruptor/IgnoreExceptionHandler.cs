using System;
using Disruptor.Logging;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using standard Console output to log the exception
    /// </summary>
    public sealed class IgnoreExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Uses a <see cref="ConsoleLogger"/> to log handled <see cref="Exception"/>s.
        /// </summary>
        public IgnoreExceptionHandler()
        {
            _logger = ConsoleLogger.Create();
        }

        /// <summary>
        /// Initialise a new instance of <see cref="IgnoreExceptionHandler"/> and use injected <see cref="ILogger"/> to log handled <see cref="Exception"/>s.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/> instance used to logged handled exceptions</param>
        public IgnoreExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Strategy for handling uncaught exceptions when processing an <see cref="IEntry"/>.
        /// If the strategy wishes to suspend further processing by the <see cref="BatchConsumer{T}"/>
        /// then is should throw a <see cref="DisruptorFatalException"/>
        /// </summary>
        /// <param name="ex">the exception that propagated from the <see cref="IBatchHandler{T}"/></param>
        /// <param name="currentEntry">currentEntry being processed when the exception occurred.</param>
        public void Handle(Exception ex, IEntry currentEntry)
        {
            _logger.Log(Level.Info, "Exception processing: " + currentEntry, ex);
        }
    }
}