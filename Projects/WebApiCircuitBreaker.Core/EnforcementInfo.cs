using System.Collections.Generic;
using System.Net;

namespace WebApiCircuitBreaker.Core
{
    /// <summary>
    /// Contains information to be returned when the circuit is open.
    /// </summary>
    public class EnforcementInfo
    {
        public HttpStatusCode ResponseCodeOnCircuitOpen { get; set; }

        public Dictionary<string, string> CustomHeaders { get; set; } 
    }
}
