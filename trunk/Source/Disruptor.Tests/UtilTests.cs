using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class UtilTests
    {
        [Test]
        public void ShouldReturnNextPowerOfTwo()
        {
            var powerOfTwo = Util.CeilingNextPowerOfTwo(1000);

            Assert.AreEqual(1024, powerOfTwo);
        }

        [Test]
        public void ShouldReturnExactPowerOfTwo()
        {
            var powerOfTwo = Util.CeilingNextPowerOfTwo(1024);

            Assert.AreEqual(1024, powerOfTwo);
        }

        [Test]
        public void ShouldReturnMinimumSequence()
        {
            var consumerMock1 = new Mock<IConsumer>();
            var consumerMock2 = new Mock<IConsumer>();
            var consumerMock3 = new Mock<IConsumer>();

            var consumers = new[] {consumerMock1.Object, consumerMock2.Object, consumerMock3.Object};

            consumerMock1.SetupGet(c => c.Sequence).Returns(11);
            consumerMock2.SetupGet(c => c.Sequence).Returns(4);
            consumerMock3.SetupGet(c => c.Sequence).Returns(13);

            Assert.AreEqual(4L, Util.GetMinimumSequence(consumers));

            consumerMock1.Verify();
            consumerMock2.Verify();
            consumerMock3.Verify();
        }
    }
}
/*
public final class UtilTest
{
    private final Mockery context = new Mockery();

    @Test
    public void shouldReturnMinimumSequence()
    {
        final Consumer[] consumers = new Consumer[3];
        consumers[0] = context.mock(Consumer.class, "c0");
        consumers[1] = context.mock(Consumer.class, "c1");
        consumers[2] = context.mock(Consumer.class, "c2");

        context.checking(new Expectations()
        {
            {
                oneOf(consumers[0]).getSequence();
                will(returnValue(Long.valueOf(7L)));

                oneOf(consumers[1]).getSequence();
                will(returnValue(Long.valueOf(3L)));

                oneOf(consumers[2]).getSequence();
                will(returnValue(Long.valueOf(12L)));
            }
        });

        Assert.assertEquals(3L, Util.getMinimumSequence(consumers));
    }

    @Test
    public void shouldReturnLongMaxWhenNoConsumers()
    {
        final Consumer[] consumers = new Consumer[0];

        Assert.assertEquals(Long.MAX_VALUE, Util.getMinimumSequence(consumers));
    }
}
*/