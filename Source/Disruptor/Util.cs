using System.Linq;

namespace Disruptor
{
    /// <summary>
    /// Set of common functions used by the Disruptor
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Calculate the next power of 2, greater than or equal to x.
        /// </summary>
        /// <param name="x">Value to round up</param>
        /// <returns>The next power of 2 from x inclusive</returns>
        public static int CeilingNextPowerOfTwo(int x)
        {
            var result = 2;

            while(result < x)
            {
                result *= 2;
            }

            return result;
        }

        /// <summary>
        /// Get the minimum sequence from an array of <see cref="IConsumer"/>s.
        /// </summary>
        /// <param name="consumers">consumers to compare.</param>
        /// <returns>the minimum sequence found or lon.MaxValue if the array is empty.</returns>
        public static long GetMinimumSequence(IConsumer[] consumers)
        {
            if (consumers.Length == 0) return long.MaxValue;

            var min = long.MaxValue;
            for (var i = 0; i < consumers.Length; i++)
            {
                var sequence = consumers[i].Sequence;
                if(sequence < min)
                {
                    min = sequence;
                }
            }
            return min;
        }
    }
}