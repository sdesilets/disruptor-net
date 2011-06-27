using System;
using Disruptor.Logging;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using <see cref="ILogger"/> to log
    /// the exception at Fatal level and re-throw it
    /// </summary>
    public sealed class FatalExceptionHandler:IExceptionHandler
    {
        private readonly ILogger _logger;

        public FatalExceptionHandler()
        {
            _logger = ConsoleLogger.Create();
        }

        public FatalExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(Exception ex, IEntry currentEntry)
        {
            var message = "Exception processing: " + currentEntry;
            _logger.Log(Level.Fatal, message, ex);

            throw new RuntimeException(message, ex);
        }
    }
}