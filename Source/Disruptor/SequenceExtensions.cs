namespace Disruptor
{
    /// <summary>
    /// Provides an extension method for <see cref="IEventProcessor"/>[]
    /// </summary>
    internal static class SequenceExtensions
    {
        /// <summary>
        /// Get the minimum sequence from an array of <see cref="Sequence"/>s.
        /// </summary>
        /// <param name="sequences">sequences to compare.</param>
        /// <returns>the minimum sequence found or lon.MaxValue if the array is empty.</returns>
        public static long GetMinimumSequence(this Sequence[] sequences)
        {
            if (sequences.Length == 0) return long.MaxValue;

            var min = long.MaxValue;
            for (var i = 0; i < sequences.Length; i++)
            {
                var sequence = sequences[i].Value; // volatile read
                min = min < sequence ? min : sequence;
            }
            return min;
        }
    }
}