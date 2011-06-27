namespace Disruptor.Tests.Support
{
    public class StubEntry : AbstractEntry
    {
        public StubEntry(int i)
        {
            Value = i;
        }

        public int Value { get; private set; }
        public string TestString { get; set; }

        public void Copy(StubEntry entry)
        {
            Value = entry.Value;
        }

        public override int GetHashCode()
        {
            //TODO original implementation is a bit wierd..
            return Value;
        }

        public bool Equals(StubEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Value == Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (StubEntry)) return false;
            return Equals((StubEntry) obj);
        }
    }
}