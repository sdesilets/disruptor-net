namespace Disruptor
{
    /// <summary>
    /// Base implementation provided for ease of use
    /// </summary>
    public abstract class AbstractEntry : IEntry
    {
        public long Sequence { get; set; }
    }
}

