using System;

namespace WebApiCircuitBreaker.Core
{
    public class RuleLoadingStrategy
    {
        public RuleLoadingIntervalEnum RuleLoadingInteval { get; set; }

        public TimeSpan RuleLoadingTimeSpan { get; set; }
    }
}
