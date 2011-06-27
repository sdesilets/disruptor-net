using System;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using standard Console output to log the exception
    /// </summary>
    public sealed class IgnoreExceptionHandler:IExceptionHandler
    {
        public void Handle(Exception ex, IEntry currentEntry)
        {
            Console.WriteLine("Info - Exception processing for entry {0} : {1}", currentEntry, ex);
        }
    }
}