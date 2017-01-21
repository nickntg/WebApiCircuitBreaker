using System;
using System.Collections.Generic;
using System.Net;
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
        public void CircuitClosesOnBlacklistedClient()
        {
            var reader = new SimpleReader(new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    RuleName = "rule1",
                    BlackList = new HashSet<string> {"abc"},
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1}
                },
                new ConfigRule {IsActive = true, RuleName = "rule2"}
            });

            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            mockFinder.Setup(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>())).Returns("abc");

            var breaker = new CircuitBreaker(reader, null, mockFinder.Object);

            breaker.Contexts.TryAdd(
                breaker.GetRuleKey(null, reader.Rules[0]),
                new CircuitBreakerContext { IsCircuitOpen = false, OpenUntil = DateTime.Now.AddDays(1) });

            Assert.IsNotNull(breaker.FindOpenCircuitContext(new HttpRequestMessage(HttpMethod.Get, string.Empty)));

            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Test]
        public void CircuitNotClosingOnClientThatIsNotBlacklistedWithActiveBlacklist()
        {
            var reader = new SimpleReader(new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    RuleName = "rule1",
                    BlackList = new HashSet<string> {"abc"},
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1}
                },
                new ConfigRule {IsActive = true, RuleName = "rule2", BlackList = new HashSet<string> {"abc"}}
            });

            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            mockFinder.Setup(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>())).Returns("abcde");

            var breaker = new CircuitBreaker(reader, null, mockFinder.Object);

            breaker.Contexts.TryAdd(
                breaker.GetRuleKey(null, reader.Rules[0]),
                new CircuitBreakerContext { IsCircuitOpen = false, OpenUntil = DateTime.Now.AddDays(1) });

            Assert.IsNull(breaker.FindOpenCircuitContext(new HttpRequestMessage(HttpMethod.Get, string.Empty)));

            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Exactly(2));
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

        #region Check circuit tests

        [Test]
        public void NothingOpensWithoutRules()
        {
            var breaker = new CircuitBreaker(new SimpleReader(new List<ConfigRule>()), null, null);

            breaker.CheckCircuit(null, null);

            Assert.AreEqual(0, breaker.Contexts.Count);
        }

        [Test]
        public void NothingOpensWithEmptyRules()
        {
            var breaker = new CircuitBreaker(
                new SimpleReader(new List<ConfigRule>
                {
                    new ConfigRule {IsActive = false},
                    new ConfigRule {IsActive = false}
                }), null, null);

            breaker.CheckCircuit(null, null);

            Assert.AreEqual(0, breaker.Contexts.Count);
        }

        [Test]
        public void NothingOpensWithSuccessfulStatusCode()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 2, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo =
                        new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable}
                }
            };
            var breaker = new CircuitBreaker(new SimpleReader(rules), null, null);

            breaker.CheckCircuit(null, new HttpResponseMessage(HttpStatusCode.Accepted));

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(0, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsFalse(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);
        }

        [Test]
        public void NothingOpensWithNotDesignatedResponseCode()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 2, LowWatermark = 1, StatusCode = HttpStatusCode.NotFound},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable}
                }
            };
            var breaker = new CircuitBreaker(new SimpleReader(rules), null, null);

            breaker.CheckCircuit(null, new HttpResponseMessage(HttpStatusCode.InternalServerError));

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(0, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsFalse(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);
        }

        [Test]
        public void NothingOpensOnNonApplicableServer()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 2, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable},
                    ApplicableServers = new HashSet<string> { "someserver" }
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            mockLogger.Setup(x => x.LogLowWatermark(It.IsAny<string>()));

            var breaker = new CircuitBreaker(new SimpleReader(rules), mockLogger.Object, null);

            breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            Assert.AreEqual(0, breaker.Contexts.Count);

            mockLogger.Verify(x => x.LogLowWatermark(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void NothingOpensWithWhitelistedClient()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 2, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable},
                    WhiteList = new HashSet<string> {"abc"}
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            mockLogger.Setup(x => x.LogLowWatermark(It.IsAny<string>()));

            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            mockFinder.Setup(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>())).Returns("abc");

            var breaker = new CircuitBreaker(new SimpleReader(rules), mockLogger.Object, mockFinder.Object);

            breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            Assert.AreEqual(0, breaker.Contexts.Count);

            mockLogger.Verify(x => x.LogLowWatermark(It.IsAny<string>()), Times.Never);
            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Test]
        public void VerifyEnteringLowWatermarkWithActiveServerList()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 2, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable},
                    ApplicableServers = new HashSet<string> { Environment.MachineName }
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            mockLogger.Setup(x => x.LogLowWatermark(It.IsAny<string>()));

            var breaker = new CircuitBreaker(new SimpleReader(rules), mockLogger.Object, null);

            breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(1, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsFalse(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);

            mockLogger.Verify(x => x.LogLowWatermark(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void VerifyEnteringLowWatermarkWithActiveWhiteListButClientNotInList()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 2, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable},
                    WhiteList = new HashSet<string> {"abc"}
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            mockLogger.Setup(x => x.LogLowWatermark(It.IsAny<string>()));

            var mockFinder = new Mock<IAddressFinder>(MockBehavior.Strict);
            mockFinder.Setup(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>())).Returns("abcde");

            var breaker = new CircuitBreaker(new SimpleReader(rules), mockLogger.Object, mockFinder.Object);

            breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(1, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsFalse(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);

            mockLogger.Verify(x => x.LogLowWatermark(It.IsAny<string>()), Times.Once);
            mockFinder.Verify(x => x.FindIpAddress(It.IsAny<HttpRequestMessage>()), Times.Once);
        }

        [Test]
        public void VerifyEnteringLowWatermark()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 2, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable}
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            mockLogger.Setup(x => x.LogLowWatermark(It.IsAny<string>()));

            var breaker = new CircuitBreaker(new SimpleReader(rules), mockLogger.Object, null);

            breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(1, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsFalse(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);

            mockLogger.Verify(x => x.LogLowWatermark(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void VerifyEnteringHighWatermark()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 3, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable}
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            mockLogger.Setup(x => x.LogLowWatermark(It.IsAny<string>()));
            mockLogger.Setup(x => x.LogCircuitOpen(It.IsAny<string>()));

            var breaker = new CircuitBreaker(new SimpleReader(rules), mockLogger.Object, null);

            for (var i = 1; i <= 3; i++)
            {
                breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                    new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(3, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsTrue(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);

            mockLogger.Verify(x => x.LogLowWatermark(It.IsAny<string>()), Times.Once);
            mockLogger.Verify(x => x.LogCircuitOpen(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void VerifyClearErrorsOnSuccessfulCall()
        {
            var rules = new List<ConfigRule>
            {
                new ConfigRule
                {
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 1, HighWatermark = 3, LowWatermark = 1},
                    RuleName = "test",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.Global,
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable}
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            mockLogger.Setup(x => x.LogLowWatermark(It.IsAny<string>()));

            var breaker = new CircuitBreaker(new SimpleReader(rules), mockLogger.Object, null);

            breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(1, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsFalse(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);

            breaker.CheckCircuit(new HttpRequestMessage(HttpMethod.Get, "http://localhost/some/url"),
                new HttpResponseMessage(HttpStatusCode.OK));

            Assert.AreEqual(1, breaker.Contexts.Count);
            Assert.AreEqual(0, breaker.Contexts[breaker.GetRuleKey(null, rules[0])].ApplicableRequests);
            Assert.IsFalse(breaker.Contexts[breaker.GetRuleKey(null, rules[0])].IsCircuitOpen);

            mockLogger.Verify(x => x.LogLowWatermark(It.IsAny<string>()), Times.Once);
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