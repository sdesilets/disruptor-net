using System.Runtime.InteropServices;

namespace Disruptor.MemoryLayout
{
    /// <summary>
    /// A <see cref="long"/> wrapped in PaddedLong is guaranteed to live on its own cache line
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 2 * CacheLine.Size)]
    public struct PaddedLong
    {
        [FieldOffset(CacheLine.Size)]
        private long _data;

        ///<summary>
        /// Initialise a new instance of CacheLineStorage
        ///</summary>
        ///<param name="data">default value of data</param>
        public PaddedLong(long data)
        {
            _data = data;
        }

        /// <summary>
        /// Expose data with full fence on read and write
        /// </summary>
        public long Data
        {
            get { return _data; }
            set { _data = value; }
        }
    }
}