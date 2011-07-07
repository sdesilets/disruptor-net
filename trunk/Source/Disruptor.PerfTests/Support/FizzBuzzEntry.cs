namespace Disruptor.PerfTests.Support
{
    public sealed class FizzBuzzEntry
    {
        public long Value { get; set; }
        public bool Fizz { get; set; }
        public bool Buzz { get; set; }

        public void Reset()
        {
            Value = 0L;
            Fizz = false;
            Buzz = false;
        }
    }
}