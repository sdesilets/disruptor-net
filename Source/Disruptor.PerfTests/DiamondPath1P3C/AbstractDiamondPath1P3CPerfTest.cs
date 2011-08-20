namespace Disruptor.PerfTests.DiamondPath1P3C
{
    public abstract class AbstractDiamondPath1P3CPerfTest:ThroughputPerfTest
    {
        protected const int Size = 1024 * 8;
        private long _expectedResult;

        protected AbstractDiamondPath1P3CPerfTest(int iterations) : base(iterations)
        {
        }

        protected long ExpectedResult
        {
            get
            {
                if (_expectedResult == 0)
                {
                    for (long i = 0; i < Iterations; i++)
                    {
                        var fizz = 0 == (i % 3L);
                        var buzz = 0 == (i % 5L);

                        if (fizz && buzz)
                        {
                            ++_expectedResult;
                        }
                    }
                }
                return _expectedResult;
            }
        }

        public override int MinimumCoresRequired
        {
            get { return 4; }
        }
    }
}