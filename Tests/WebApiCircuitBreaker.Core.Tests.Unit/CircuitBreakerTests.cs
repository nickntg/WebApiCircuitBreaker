using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using NUnit.Framework;
using WebApiCircuitBreaker.Core.Interfaces;
using WebApiCircuitBreaker.Extensions.Readers;

namespace WebApiCircuitBreaker.Core.Tests.Unit
{
    [TestFixture]
    public class CircuitBreakerTests
    {
        #region Find open circuit tests

        [Test]
        public void NothingFoundWithNoRules()
        {
            var breaker = new CircuitBreaker(new SimpleReader(new List<ConfigRule>()), null, null);

            Assert.IsNull(breaker.FindOpenCircuitContext(null));
        }

        [Test]
        public void NothingHappensWithInactiveRules()
        {
            var breaker = new CircuitBreaker(
                new SimpleReader(new List<ConfigRule>
                {
                    new ConfigRule {IsActive = false},
                    new ConfigRule {IsActive = false}
                }), null, null);

            Assert.IsNull(breaker.FindOpenCircuitContext(null));
        }

        [Test]
        public void NothingHappensWithoutExistingContexts()
        {
            var breaker = new CircuitBreaker(
                new SimpleReader(new List<ConfigRule>
                {
                    new ConfigRule {IsActive = true, RuleName = "rule1"},
                    new ConfigRule {IsActive = true, RuleName = "rule2"}
                }), null, null);

            Assert.IsNull(breaker.FindOpenCircuitContext(null));
        }

        [Test]
        public void NothingHappensWithContextThatDoesNotDesignateAnOpenCircuit()
        {
            var reader = new SimpleReader(new List<ConfigRule>
            {
                new ConfigRule {IsActive = true, RuleName = "rule1"},
                new ConfigRule {IsActive = true, RuleName = "rule2"}
            });

            var breaker = new CircuitBreaker(reader, null, null);

            breaker.Contexts.TryAdd(
                breaker.GetRuleKey(null, reader.Rules[0]),
                new CircuitBreakerContext {IsCircuitOpen = false, OpenUntil = DateTime.Now.AddDays(1)});

            Assert.IsNull(breaker.FindOpenCircuitContext(null));
        }

        [Test]
        public void NothingHappensWithContextThatDesignatesRuleThatHasLapsed()
        {
            var reader = new SimpleReader(new List<ConfigRule>
            {
                new ConfigRule {IsActive = true, RuleName = "rule1"},
                new ConfigRule {IsActive = true, RuleName = "rule2"}
            });

            var breaker = new CircuitBreaker(reader, null, null);

            breaker.Contexts.TryAdd(
                breaker.GetRuleKey(null, reader.Rules[0]),
                new CircuitBreakerContext { IsCircuitOpen = true, OpenUntil = DateTime.Now.AddDays(-1) });

            Assert.IsNull(breaker.FindOpenCircuitContext(null));
        }

        [Test]
        public void FindFirstOpenCircuit()
        {
            var reader = new SimpleReader(new List<ConfigRule>
            {
                new ConfigRule {IsActive = true, RuleName = "rule1"},
                new ConfigRule {IsActive = true, RuleName = "rule2"}
            });

            var breaker = new CircuitBreaker(reader, null, null);

            breaker.Contexts.TryAdd(
                breaker.GetRuleKey(null, reader.Rules[0]),
                new CircuitBreakerContext { IsCircuitOpen = true, OpenUntil = DateTime.Now.AddDays(1) });

            Assert.AreSame(breaker.Contexts[breaker.GetRuleKey(null, reader.Rules[0])], breaker.FindOpenCircuitContext(null));
        }

        [Test]
        public void FindNextOpenCircuit()
        {
            var reader = new SimpleReader(new List<ConfigRule>
            {
                new ConfigRule {IsActive = true, RuleName = "rule1"},
                new ConfigRule {IsActive = true, RuleName = "rule2"}
            });

            var breaker = new CircuitBreaker(reader, null, null);

            breaker.Contexts.TryAdd(
                breaker.GetRuleKey(null, reader.Rules[0]),
                new CircuitBreakerContext { IsCircuitOpen = false, OpenUntil = DateTime.Now.AddDays(1) });

            breaker.Contexts.TryAdd(
                breaker.GetRuleKey(null, reader.Rules[1]),
                new CircuitBreakerContext { IsCircuitOpen = true, OpenUntil = DateTime.Now.AddDays(1) });

            Assert.AreSame(breaker.Contexts[breaker.GetRuleKey(null, reader.Rules[1])], breaker.FindOpenCircuitContext(null));
        }

        #endregion

        #region Rule key creation tests

        [Test]
        public void EnsureSimpleRuleKeyIsValid()
        {
            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            var breaker = new CircuitBreaker(new EmptyReader(), null, mockFinder.Object);
            var msg = new HttpRequestMessage();
            var rule = new ConfigRule
            {
                RuleName = "test",
                ApplicabilityScope = ApplicabilityScopeEnum.Global,
                RouteScope = RouteScopeEnum.Global
            };

            Assert.AreEqual("test__", breaker.GetRuleKey(msg, rule));

            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Never());
        }

        [Test]
        public void EnsurePerRouteRuleKeyIsValid()
        {
            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            var breaker = new CircuitBreaker(new EmptyReader(), null, mockFinder.Object);
            var msg = new HttpRequestMessage {RequestUri = new Uri("http://localhost/some/url/?param=value")};
            var rule = new ConfigRule
            {
                RuleName = "test",
                ApplicabilityScope = ApplicabilityScopeEnum.Global,
                RouteScope = RouteScopeEnum.PerRoute
            };

            Assert.AreEqual("test__/some/url/", breaker.GetRuleKey(msg, rule));

            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Never());
        }

        [Test]
        public void EnsurePerClientRuleKeyIsValid()
        {
            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            mockFinder.Setup(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>())).Returns("::1");

            var breaker = new CircuitBreaker(new EmptyReader(), null, mockFinder.Object);
            var msg = new HttpRequestMessage();
            var rule = new ConfigRule
            {
                RuleName = "test",
                ApplicabilityScope = ApplicabilityScopeEnum.PerClient,
                RouteScope = RouteScopeEnum.Global
            };

            Assert.AreEqual("test_::1_", breaker.GetRuleKey(msg, rule));

            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Test]
        public void EnsurePerClientPerRouteRuleKeyIsValid()
        {
            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            mockFinder.Setup(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>())).Returns("::1");

            var breaker = new CircuitBreaker(new EmptyReader(), null, mockFinder.Object);
            var msg = new HttpRequestMessage { RequestUri = new Uri("http://localhost/some/url/?param=value") };
            var rule = new ConfigRule
            {
                RuleName = "test",
                ApplicabilityScope = ApplicabilityScopeEnum.PerClient,
                RouteScope = RouteScopeEnum.PerRoute
            };

            Assert.AreEqual("test_::1_/some/url/", breaker.GetRuleKey(msg, rule));

            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        #endregion
    }

    internal class SimpleReader : IRuleReader
    {
        public IList<ConfigRule> Rules;

        public SimpleReader(IList<ConfigRule> rules)
        {
            Rules = rules;
        }

        public IList<ConfigRule> ReadConfigRules()
        {
            return Rules;
        }
    }
}