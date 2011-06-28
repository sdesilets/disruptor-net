using System;
using Disruptor.Logging;
using Disruptor.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class FatalExceptionHandlerTests
    {
        [Test]
        public void ShouldHandleFatalException()
        {
            var loggerMock = new Mock<ILogger>();
            var exception = new Exception();
            var exceptionHandler = new FatalExceptionHandler(loggerMock.Object);
            var entry = new StubEntry(-1);

            try
            {
                exceptionHandler.Handle(exception, entry);
            }
            catch (DisruptorFatalException e)
            {
                Assert.AreSame(exception, e.InnerException);
            }

            loggerMock.Verify(logger=> logger.Log(Level.Fatal, "Exception processing: " + entry, exception));
        }
    }
}