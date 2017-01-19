using System.Net;

namespace WebApiCircuitBreaker.Core.Extensions
{
    public static class HttpStatusCodeExtensions
    {
        public static bool IsSuccessful(this HttpStatusCode code)
        {
            return (int) code < 500;
        }
    }
}