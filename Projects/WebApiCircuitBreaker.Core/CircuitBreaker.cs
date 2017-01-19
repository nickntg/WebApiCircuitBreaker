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
        private readonly object _monitor = new object();

        public readonly ConcurrentDictionary<string, CircuitBreakerContext> Contexts;

        public CircuitBreaker(IRuleReader reader, ILogger logger, IAddressFinder addressFinder)
        {
            Contexts = new ConcurrentDictionary<string, CircuitBreakerContext>();
            _rules = reader.ReadConfigRules();
            _logger = logger;
            _addressFinder = addressFinder;
        }

        public CircuitBreakerContext FindOpenCircuitContext(HttpRequestMessage request)
        {
            // TODO: Need to skip whitelisted IPs and block blacklisted IPs.
            // TODO: Need to check if a server list is specified.
            foreach (var rule in _rules)
            {
                // Ignore inactive rules.
                if (!rule.IsActive)
                {
                    continue;
                }

                var key = GetRuleKey(request, rule);
                CircuitBreakerContext context;
                if (Contexts.TryGetValue(key, out context))
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

        public void CheckCircuit(HttpRequestMessage request, HttpResponseMessage response)
        {
            foreach (var rule in _rules)
            {
                if (!rule.IsActive)
                {
                    continue;
                }

                var key = GetRuleKey(request, rule);
                CircuitBreakerContext context;

                if (!Contexts.TryGetValue(key, out context) || context.LastUpdate.Subtract(DateTime.Now).Seconds > 5*60)
                {
                    context = new CircuitBreakerContext
                    {
                        ApplicableRequests = 0,
                        ApplicableRule = rule,
                        IsCircuitOpen = false
                    };
                }

                // http://www.interact-sw.co.uk/iangblog/2004/03/23/locking
                if (!Monitor.TryEnter(_monitor, 1000))
                {
                    _logger?.LogUnexpectedError("Failed to enter monitor in 1 second - bad things are happenning.");
                }
                else
                {
                    try
                    {
                        if ((rule.LimitInfo.StatusCode.HasValue && rule.LimitInfo.StatusCode == response.StatusCode) ||
                            (!rule.LimitInfo.StatusCode.HasValue && !response.IsSuccessStatusCode))
                            //TODO: This is not exactly right for these purposes...404 is not a successful code according 
                            //TODO: to this but it can be validly returned by an API...need to write a custom method.
                        {
                            // Error condition matching the rule.
                            context.ApplicableRequests++;
                            if (context.ApplicableRequests == rule.LimitInfo.LowWatermark)
                            {
                                _logger?.LogLowWatermark($"Rule {rule.RuleName} - low watermark exceeded for {request.RequestUri.AbsolutePath}, server {Environment.MachineName}");
                            }
                            else if (context.ApplicableRequests >= rule.LimitInfo.HighWatermark)
                            {
                                // Just log the circuit open message but don't do anything in this request
                                // because it has already been computed - the next one will be stoped.
                                _logger?.LogCircuitOpen($"Rule {rule.RuleName} - high watermark exceeded for {request.RequestUri.AbsolutePath}, server {Environment.MachineName}");
                                context.IsCircuitOpen = true;
                                context.OpenUntil = DateTime.Now.AddSeconds(rule.LimitInfo.BreakerIntervalInSeconds);
                            }
                        }
                        else
                        {
                            // Need to reset counters.
                            context.ApplicableRequests = 0;
                        }

                        context.LastUpdate = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogUnexpectedError(ex.ToString());
                    }
                    finally
                    {
                        Monitor.Exit(_monitor);
                    }
                }

                Contexts.AddOrUpdate(key, context, (s, breakerContext) => context);
            }
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

            // Check if we need to open the circuit for subsequent requests.
            CheckCircuit(request, response);

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