using System.Net;
using NUnit.Framework;
using WebApiCircuitBreaker.Core.Extensions;

namespace WebApiCircuitBreaker.Core.Tests.Unit
{
    [TestFixture]
    public class ExtensionTests
    {
        [TestCase(HttpStatusCode.ServiceUnavailable, false)]
        [TestCase(HttpStatusCode.InternalServerError, false)]
        [TestCase(HttpStatusCode.NotFound, true)]
        [TestCase(HttpStatusCode.OK, true)]
        [TestCase(HttpStatusCode.BadRequest, true)]
        public void TestSuccessfulStatusCode(HttpStatusCode code, bool expected)
        {
            Assert.AreEqual(expected, code.IsSuccessful());
        }
    }
}
