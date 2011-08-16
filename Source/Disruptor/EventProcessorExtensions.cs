namespace Disruptor
{
    /// <summary>
    /// Provides an extension method for <see cref="IEventProcessor"/>[]
    /// </summary>
    public static class EventProcessorExtensions
    {
        /// <summary>
        /// Get the minimum sequence from an array of <see cref="IEventProcessor"/>s.
        /// </summary>
        /// <param name="eventProcessors">eventProcessors to compare.</param>
        /// <returns>the minimum sequence found or lon.MaxValue if the array is empty.</returns>
        public static long GetMinimumSequence(this IEventProcessor[] eventProcessors)
        {
            if (eventProcessors.Length == 0) return long.MaxValue;

            var min = long.MaxValue;
            for (var i = 0; i < eventProcessors.Length; i++)
            {
                var sequence = eventProcessors[i].Sequence; // volatile read
                min = min < sequence ? min : sequence;
            }
            return min;
        }
    }
}