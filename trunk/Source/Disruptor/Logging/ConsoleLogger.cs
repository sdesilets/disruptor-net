using System;

namespace Disruptor.Logging
{
    /// <summary>
    /// Simple console logger implementation
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public static ILogger Create()
        {
            return new ConsoleLogger();
        }

        public void Log(Level level, string message, Exception ex)
        {
            Console.WriteLine("{0} - {1} - Exception: {2}", level, message, ex);
        }
    }
}