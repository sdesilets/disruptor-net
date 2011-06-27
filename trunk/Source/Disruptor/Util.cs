using System.Linq;

namespace Disruptor
{
    public static class Util
    {
        /// <summary>
        /// Calculate the next power of 2, greater than or equal to x.
        /// From Hacker's Delight, Chapter 3, Harry S. Warren Jr.
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

        public static long GetMinimumSequence(IConsumer[] consumers)
        {
            //TODO convert to extension method
            return consumers.Length == 0 ? long.MaxValue : consumers.Min(c => c.Sequence);
        }
    }
}