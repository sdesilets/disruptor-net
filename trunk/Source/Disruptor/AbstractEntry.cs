namespace Disruptor
{
    /// <summary>
    /// Base implementation provided for ease of use
    /// </summary>
    public abstract class AbstractEntry : IEntry
    {
        /// <summary>
        /// get: Get the sequence number assigned to this item in the series.
        /// set: Explicitly set the sequence number for this Entry and a CommitCallback for indicating when the producer is
        ///      finished with assigning data for exchange.
        /// </summary>
        public long Sequence { get; set; }
    }
}

