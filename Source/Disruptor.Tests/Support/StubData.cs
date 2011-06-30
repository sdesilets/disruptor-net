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

        public override string ToString()
        {
            return string.Format("Value: {0}, TestString: {1}", Value, TestString);
        }
    }
}