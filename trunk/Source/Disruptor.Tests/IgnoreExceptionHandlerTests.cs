using System;
using Disruptor.Logging;
using Disruptor.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class IgnoreExceptionHandlerTests
    {
        [Test]
        public void ShouldHandleAndIgnoreException()
        {
            var loggerMock = new Mock<ILogger>();
            var ex = new Exception();
            var entry = new StubEntry(1);
            var exceptionHandler = new IgnoreExceptionHandler(loggerMock.Object);

            //check no exception bubble here
            exceptionHandler.Handle(ex, entry);

            loggerMock.Verify(logger => logger.Log(Level.Info, "Exception processing: " + entry, ex));
        }
    }
}
