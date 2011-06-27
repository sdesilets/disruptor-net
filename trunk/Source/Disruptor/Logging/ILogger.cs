using System;

namespace Disruptor.Logging
{
    /// <summary>
    /// Lightweigt logging interface (No dependency on  third party logging framework)
    /// </summary>
    public interface ILogger
    {
        void Log(Level level, string message, Exception ex);
    }
}