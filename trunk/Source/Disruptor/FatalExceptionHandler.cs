using System;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using standard Console output to log
    /// the exception and re-throw it
    /// </summary>
    public sealed class FatalExceptionHandler:IExceptionHandler
    {
        public void Handle(Exception ex, IEntry currentEntry)
        {
            Console.WriteLine("Error - Exception processing for entry {0} : {1}", currentEntry, ex);

            //TODO check if we need to wrap the exception (see default implem)
            throw ex;
        }
    }
}