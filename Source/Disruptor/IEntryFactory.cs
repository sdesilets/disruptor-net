namespace Disruptor
{
    /// <summary>
    /// Called by the <see cref="RingBuffer{T}"/> to pre-populate all the <see cref="IEntry"/>s to fill the RingBuffer.
    /// </summary>
    /// <typeparam name="T"> Entry implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IEntryFactory<out T> where T:IEntry
    {
        //TODO check why original implementation does not constraint T to implement IEntry

        T Create();
    }
}