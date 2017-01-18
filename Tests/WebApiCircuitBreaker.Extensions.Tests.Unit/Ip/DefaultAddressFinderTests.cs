using System.Net.Http;
using System.Web;
using Moq;
using NUnit.Framework;
using WebApiCircuitBreaker.Extensions.Ip;

namespace WebApiCircuitBreaker.Extensions.Tests.Unit.Ip
{
    [TestFixture]
    public class DefaultAddressFinderTests
    {
        [Test]
        public void NoRequestOrXForwardedForReturnsEmptyString()
        {
            var msg = new HttpRequestMessage();
            var finder = new DefaultAddressFinder();

            Assert.IsTrue(string.IsNullOrEmpty(finder.FindIpAddress(msg)));
        }

        [Test]
        public void FindUserHostAddressFromMsHttpContext()
        {
            var msg = new HttpRequestMessage();

            var mockContext = new Mock<FakeHttpContext>(MockBehavior.Strict);
            mockContext.SetupGet(x => x.Request.UserHostAddress).Returns("::1");
            msg.Properties.Add("MS_HttpContext", mockContext.Object);

            var finder = new DefaultAddressFinder();

            Assert.AreEqual("::1", finder.FindIpAddress(msg));

            mockContext.VerifyGet(x => x.Request.UserHostAddress, Times.Once);
        }

        [Test]
        public void XForwardedForExistsButIsNull()
        {
            var msg = new HttpRequestMessage();
            msg.Headers.Add("X-Forwarded-For", string.Empty);

            var mockContext = new Mock<FakeHttpContext>(MockBehavior.Strict);
            mockContext.SetupGet(x => x.Request.UserHostAddress).Returns("::1");

            var finder = new DefaultAddressFinder();

            Assert.IsTrue(string.IsNullOrEmpty(finder.FindIpAddress(msg)));

            mockContext.VerifyGet(x => x.Request.UserHostAddress, Times.Never);
        }

        [Test]
        public void GetHeaderForXFo()
        {
            var msg = new HttpRequestMessage();
            msg.Headers.Add("X-Forwarded-For", "129.78.138.66, 129.78.64.103");

            var mockContext = new Mock<FakeHttpContext>(MockBehavior.Strict);
            mockContext.SetupGet(x => x.Request.UserHostAddress).Returns("::1");

            var finder = new DefaultAddressFinder();

            Assert.AreEqual("129.78.64.103", finder.FindIpAddress(msg));

            mockContext.VerifyGet(x => x.Request.UserHostAddress, Times.Never);
        }
    }

    public class FakeHttpContext : HttpContextBase
    {
        public override HttpRequestBase Request { get; }
    }
}
