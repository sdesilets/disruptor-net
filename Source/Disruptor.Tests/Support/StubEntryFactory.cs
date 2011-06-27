namespace Disruptor.Tests.Support
{
    public class StubEntryFactory:IEntryFactory<StubEntry>
    {
        public StubEntry Create()
        {
            return new StubEntry(-1);
        }
    }
}