using System.Runtime.InteropServices;
using System.Threading;

namespace Disruptor.MemoryLayout
{
    /// <summary>
    /// A <see cref="long"/> wrapped in CacheLineStorageLong is guaranteed to live on its own cache line
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 2 * CacheLine.Size)]
    internal struct CacheLineStorageAtomicLong
    {
        [FieldOffset(CacheLine.Size)]
        private long _data;

        ///<summary>
        /// Initialise a new instance of CacheLineStorage
        ///</summary>
        ///<param name="data">default value of data</param>
        public CacheLineStorageAtomicLong(long data)
        {
            _data = data;
        }

        ///<summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        ///</summary>
        ///<returns>incremented result</returns>
        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref _data);
        }

        /// <summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        public long IncrementAndGet(int delta)
        {
            return Interlocked.Add(ref _data, delta);
        }
    }
}