using System;
using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class IgnoreExceptionHandlerTests
    {
        [Test]
        //TODO review IgnoreExceptionHandler to be able to test the handling (we can't fake the static call ATM), 
        //this test just check that IgnoreExceptionHandler properly ignore the exception
        public void ShouldHandleAndIgnoreException()
        {
            var ex = new Exception();
            var entry = new StubEntry(1);

            var exceptionHandler = new IgnoreExceptionHandler();
            exceptionHandler.Handle(ex, entry);
        }
    }
}
