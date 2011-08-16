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
            var eventProcessorMock1 = new Mock<IEventProcessor>();
            var eventProcessorMock2 = new Mock<IEventProcessor>();
            var eventProcessorMock3 = new Mock<IEventProcessor>();

            var eventProcessors = new[] {eventProcessorMock1.Object, eventProcessorMock2.Object, eventProcessorMock3.Object};

            eventProcessorMock1.SetupGet(c => c.Sequence).Returns(11);
            eventProcessorMock2.SetupGet(c => c.Sequence).Returns(4);
            eventProcessorMock3.SetupGet(c => c.Sequence).Returns(13);

            Assert.AreEqual(4L, eventProcessors.GetMinimumSequence());

            eventProcessorMock1.Verify();
            eventProcessorMock2.Verify();
            eventProcessorMock3.Verify();
        }

        [Test]
        public void ShouldReturnLongMaxWhenNoEventProcessors()
        {
            var eventProcessors = new IEventProcessor[0];

            Assert.AreEqual(long.MaxValue, eventProcessors.GetMinimumSequence());
        }
    }
}