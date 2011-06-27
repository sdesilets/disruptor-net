using System;
using Disruptor.Logging;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using standard Console output to log the exception
    /// </summary>
    public sealed class IgnoreExceptionHandler:IExceptionHandler
    {
        private readonly ILogger _logger;

        public IgnoreExceptionHandler()
        {
            _logger = ConsoleLogger.Create();
        }

        public IgnoreExceptionHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void Handle(Exception ex, IEntry currentEntry)
        {
            _logger.Log(Level.Info, "Exception processing: " + currentEntry, ex);
        }
    }
}