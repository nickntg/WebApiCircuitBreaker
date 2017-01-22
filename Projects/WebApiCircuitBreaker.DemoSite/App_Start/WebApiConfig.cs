using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using WebApiCircuitBreaker.Core;
using WebApiCircuitBreaker.Core.Interfaces;
using WebApiCircuitBreaker.Extensions.Ip;

namespace WebApiCircuitBreaker.DemoSite
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.MessageHandlers.Add(new CircuitBreaker(new SimpleReader(), null, new DefaultAddressFinder(),
                new RuleLoadingStrategy
                {
                    RuleLoadingInteval = RuleLoadingIntervalEnum.LoadAndRefreshPeriodically,
                    RuleLoadingTimeSpan = new TimeSpan(0, 0, 5, 0)
                }));
        }
    }

    public class SimpleReader : IRuleReader
    {
        public IList<ConfigRule> ReadConfigRules(string machineName)
        {
            return new List<ConfigRule>
            {
                new ConfigRule
                {
                    RuleName = "simple rule",
                    ApplicabilityScope = ApplicabilityScopeEnum.Global,
                    RouteScope = RouteScopeEnum.PerRoute,
                    IsActive = true,
                    LimitInfo = new LimitInfo {BreakerIntervalInSeconds = 20, LowWatermark = 1, HighWatermark = 2},
                    EnforcementInfo = new EnforcementInfo {ResponseCodeOnCircuitOpen = HttpStatusCode.ServiceUnavailable}
                }
            };
        }
    }
}
