using System.Runtime.InteropServices;
using System.Threading;

namespace Disruptor.MemoryLayout
{
    /// <summary>
    /// A <see cref="long"/> wrapped in CacheLineStorage is guaranteed to live on its own cache line
    /// </summary>
    /// <remarks>
    /// http://drdobbs.com/go-parallel/article/217500206?pgno=4
    /// Herb Sutter: It may seem strange that this code actually allocates enough space for two cache lines' 
    /// worth of data instead of just one. That's because, on .NET, you can't specify the alignment 
    /// of data beyond some inherent 4-byte and 8-byte alignment guarantees, which aren't big enough 
    /// for our purposes. Even if you could specify a starting alignment, the compacting garbage 
    /// collector is likely to move your object and thus change its alignment dynamically. 
    /// Without alignment to guarantee the starting address of the data, the only way to deal with 
    /// this is to allocate enough space both before and after data to ensure 
    /// that no other objects can share the cache line. 
    /// 
    /// Why not a generic version of CacheLineStorage? Because: 
    /// System.TypeLoadException : Could not load type 'Disruptor.CacheLineStorage{T} from 
    /// assembly 'Disruptor' because generic types cannot have explicit layout. 
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 2 * CacheLine.Size)]
    public struct CacheLineStorageLong
    {
        [FieldOffset(CacheLine.Size)]
        private long _data;

        ///<summary>
        /// Initialise a new instance of CacheLineStorage
        ///</summary>
        ///<param name="data">default value of data</param>
        public CacheLineStorageLong(long data)
        {
            _data = data;
        }

        /// <summary>
        /// Expose data with full fence on read and write
        /// </summary>
        public long Data
        {
            get
            {
                Thread.MemoryBarrier();
                return _data;
            }
            set
            {
                Thread.MemoryBarrier();
                _data = value;
            }
        }
    }
}