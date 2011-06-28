namespace Disruptor.PerfTests.Support
{
    public sealed class ValueEntryFactory:IEntryFactory<ValueEntry>
    {
        public ValueEntry Create()
        {
            return new ValueEntry();
        }
    }
}