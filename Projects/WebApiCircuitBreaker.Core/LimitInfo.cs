using System.Net;

namespace WebApiCircuitBreaker.Core
{
    /// <summary>
    /// Contains information that controls what the circuit breaker looks for
    /// and also determines how long the circuit will stay open.
    /// </summary>
    public class LimitInfo
    {
        public HttpStatusCode? StatusCode { get; set; }

        public int LowWatermark { get; set; }

        public int HighWatermark { get; set; }

        public int BreakerIntervalInSeconds { get; set; }
    }
}
