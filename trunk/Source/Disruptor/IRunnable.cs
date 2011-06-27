namespace Disruptor
{
    /// <summary>
    /// Equivalent of the Java Runnable interface: see http://download.oracle.com/javase/1.4.2/docs/api/java/lang/Runnable.html
    /// </summary>
    public interface IRunnable
    {
        /// <summary>
        /// When an object implementing interface <see cref="IRunnable"/> is used to create a thread, starting the thread
        /// causes the object's run method to be called in that separately executing thread.
        /// The general contract of the method run is that it may take any action whatsoever. 
        /// </summary>
        void Run();
    }
}