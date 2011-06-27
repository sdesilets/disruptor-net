namespace Disruptor
{
    /// <summary>
    ///  Implementations translate a other data representations into <see cref="IEntry"/>s claimed from the <see cref="RingBuffer{T}"/>
    /// </summary>
    /// <typeparam name="T">Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IEntryTranslator<T> where T:IEntry
    {
        /// <summary>
        /// Translate a data representation into fields set in given <see cref="IEntry"/>
        /// </summary>
        /// <param name="entry">entry into which the data should be translated.</param>
        /// <returns>the resulting entry after it has been updated.</returns>
        T TranslateTo(T entry);
    }
}