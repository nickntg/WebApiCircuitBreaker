using System.Web.Http;
using WebApiCircuitBreaker.Core;
using WebApiCircuitBreaker.Extensions.Ip;
using WebApiCircuitBreaker.Extensions.Readers;

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

            config.MessageHandlers.Add(new CircuitBreaker(new EmptyReader(), null, new DefaultAddressFinder()));
        }
    }
}
