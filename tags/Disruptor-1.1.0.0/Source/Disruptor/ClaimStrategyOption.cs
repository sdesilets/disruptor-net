namespace Disruptor
{
    /// <summary>
    /// Indicates the threading policy to be applied for claiming <see cref="Entry{T}"/>s by producers to the <see cref="RingBuffer{T}"/>.
    /// </summary>
    public enum ClaimStrategyOption
    {
        /// <summary>
        /// Strategy to be used when there are multiple producer threads claiming entries.
        /// </summary>
        MultipleProducers,
        /// <summary>
        /// Optimised strategy can be used when there is a single producer thread claiming entries.
        /// </summary>
        SingleProducer
    }
}