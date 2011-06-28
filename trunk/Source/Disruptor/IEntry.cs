namespace Disruptor
{
    /// <summary>
    /// Entries are the items exchanged via a RingBuffer.
    /// </summary>
    public interface IEntry
    {
        /// <summary>
        /// get: Get the sequence number assigned to this item in the series.
        /// set: Explicitly set the sequence number for this Entry and a CommitCallback for indicating when the producer is
        ///      finished with assigning data for exchange.
        /// </summary>
        long Sequence { get; set; }
    }
}
