using System;

namespace WebApiCircuitBreaker.Core
{
    public class CircuitBreakerContext
    {
        public ConfigRule ApplicableRule { get; set; }

        public int ApplicableRequests { get; set; }

        public bool IsCircuitOpen { get; set; }

        public DateTime OpenUntil { get; set; }

        public DateTime LastUpdate { get; set; }
    }
}
