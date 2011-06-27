using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class EntryTranslatorTests
    {
        private const string TestString = "Foo";

        [Test]
        public void ShouldTranslateOtherDataIntoAnEntry()
        {
            var factory = new StubEntryFactory();
            var entry = factory.Create();

            var entryTranslator = new ExampleEntryTranslator(TestString);

            entry = entryTranslator.TranslateTo(entry);

            Assert.AreEqual(TestString, entry.TestString);
        }

        public class ExampleEntryTranslator:IEntryTranslator<StubEntry>
        {
            private readonly string _testString;

            public ExampleEntryTranslator(string testString)
            {
                _testString = testString;
            }

            public StubEntry TranslateTo(StubEntry entry)
            {
                entry.TestString = _testString;
                return entry;
            }
        }
    }
}