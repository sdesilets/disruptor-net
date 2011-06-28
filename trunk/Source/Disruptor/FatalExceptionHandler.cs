using System;
using Disruptor.Logging;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using <see cref="ILogger"/> to log
    /// the exception at Fatal level and re-throw it
    /// </summary>
    public sealed class FatalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Uses a <see cref="ConsoleLogger"/> to log handled <see cref="Exception"/>s.
        /// </summary>
        public FatalExceptionHandler()
        {
            _logger = ConsoleLogger.Create();
        }

        /// <summary>
        /// Initialise a new instance of <see cref="FatalExceptionHandler"/> and use injected <see cref="ILogger"/> to log handled <see cref="Exception"/>s.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/> instance used to logged handled exceptions</param>
        public FatalExceptionHandler(ILogger logger)
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
            var message = "Exception processing: " + currentEntry;
            _logger.Log(Level.Fatal, message, ex);

            throw new DisruptorFatalException(message, ex);
        }
    }
}