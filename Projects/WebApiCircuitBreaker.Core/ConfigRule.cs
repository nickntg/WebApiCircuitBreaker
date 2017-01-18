using System.Collections.Generic;

namespace WebApiCircuitBreaker.Core
{
    /// <summary>
    /// Holds a configuration rule for the circuit breaker.
    /// </summary>
    public class ConfigRule
    {
        public string RuleName { get; set; }

        public bool IsActive { get; set; }

        public HashSet<string> ApplicableServers { get; set; } 

        public ApplicabilityScopeEnum ApplicabilityScope { get; set; }

        public RouteScopeEnum RouteScope { get; set; }

        public HashSet<string> WhiteList { get; set; } 

        public HashSet<string> BlackList { get; set; } 

        public LimitInfo LimitInfo { get; set; }

        public EnforcementInfo EnforcementInfo { get; set; }
    }
}
