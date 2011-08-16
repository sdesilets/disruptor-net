namespace Disruptor
{
    /// <summary>
    /// Indicates the threading policy to be applied for claiming <see cref="Event{T}"/>s by producers to the <see cref="RingBuffer{T}"/>.
    /// </summary>
    public enum ClaimStrategyOption
    {
        /// <summary>
        /// Strategy to be used when there are multiple producer threads claiming events.
        /// </summary>
        MultipleProducers,
        /// <summary>
        /// Optimised strategy can be used when there is a single producer thread claiming events.
        /// </summary>
        SingleProducer
    }
}