namespace Disruptor.Tests.Support
{
    public class StubData
    {
        public StubData(int i)
        {
            Value = i;
        }

        public int Value { get; set; }
        public string TestString { get; set; }

        public override int GetHashCode()
        {
            return Value;
        }

        public bool Equals(StubData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Value == Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(StubData)) return false;
            return Equals((StubData)obj);
        }

        public override string ToString()
        {
            return string.Format("Value: {0}, TestString: {1}", Value, TestString);
        }
    }
}