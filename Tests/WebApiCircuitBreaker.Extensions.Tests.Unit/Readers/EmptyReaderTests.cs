using NUnit.Framework;
using WebApiCircuitBreaker.Extensions.Readers;

namespace WebApiCircuitBreaker.Extensions.Tests.Unit.Readers
{
    [TestFixture]
    public class EmptyReaderTests
    {
        [Test]
        public void EnsureEmptyReaderReturnsEmptyList()
        {
            var reader = new EmptyReader();
            var list = reader.ReadConfigRules();

            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }
    }
}
