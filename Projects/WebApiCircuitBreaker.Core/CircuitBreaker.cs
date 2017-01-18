using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApiCircuitBreaker.Core.Interfaces;

namespace WebApiCircuitBreaker.Core
{
    public class CircuitBreaker : DelegatingHandler
    {
        private readonly IList<ConfigRule> _rules;
        private readonly ILogger _logger;
        private readonly IAddressFinder _addressFinder;

        private readonly ConcurrentDictionary<string, CircuitBreakerContext> _contexts;

        public CircuitBreaker(IRuleReader reader, ILogger logger, IAddressFinder addressFinder)
        {
            _contexts = new ConcurrentDictionary<string, CircuitBreakerContext>();
            _rules = reader.ReadConfigRules();
            _logger = logger;
            _addressFinder = addressFinder;
        }

        public CircuitBreakerContext FindOpenCircuitContext(HttpRequestMessage request)
        {
            foreach (var rule in _rules)
            {
                // Ignore inactive rules.
                if (!rule.IsActive)
                {
                    continue;
                }

                var key = GetRuleKey(request, rule);
                CircuitBreakerContext context;
                if (_contexts.TryGetValue(key, out context))
                {
                    // Is circuit open?
                    if (context.IsCircuitOpen)
                    {
                        // Has time lapsed?
                        if (DateTime.Now.CompareTo(context.OpenUntil) >= 0)
                        {
                            context.IsCircuitOpen = false;
                            context.ApplicableRequests = 0;
                            _logger?.LogCircuitClosed($"Rule {rule.RuleName} - circuit closing for {request.RequestUri.AbsolutePath}, server {Environment.MachineName}");
                        }
                        else
                        {
                            // Not lapsed - return a response designating an open circuit.
                            return context;
                        }
                    }
                }
            }

            return null;
        }

        public string GetRuleKey(HttpRequestMessage request, ConfigRule rule)
        {
            return $"{rule.RuleName}_" +
                $"{(rule.ApplicabilityScope == ApplicabilityScopeEnum.PerClient ? _addressFinder.FindIpAddress(request) : string.Empty)}_" +
                $"{(rule.RouteScope == RouteScopeEnum.PerRoute ? request.RequestUri.AbsolutePath : string.Empty)}";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Check if we have an open circuit.
            var openContext = FindOpenCircuitContext(request);
            if (openContext != null)
            {
                return await CreateResponse(openContext.ApplicableRule.EnforcementInfo.ResponseCodeOnCircuitOpen,
                                openContext.OpenUntil, openContext.ApplicableRule.EnforcementInfo.CustomHeaders);
            }

            // Process the request.
            var response = await base.SendAsync(request, cancellationToken);

            // Return the response.
            return response;
        }

        private Task<HttpResponseMessage> CreateResponse(HttpStatusCode statusCode, DateTime openUntil, Dictionary<string, string> headers)
        {
            var response = new HttpResponseMessage(statusCode);
            response.Headers.Add("Retry-After", new[] { openUntil.Subtract(DateTime.Now).Seconds.ToString(CultureInfo.InvariantCulture) });
            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    response.Headers.Add(key, new[] { headers[key] });
                }
            }
            var tsc = new TaskCompletionSource<HttpResponseMessage>();
            tsc.SetResult(response);
            return tsc.Task;
        }
    }
}