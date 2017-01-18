namespace WebApiCircuitBreaker.Core
{
    /// <summary>
    /// Determines the scope of the circuit breaker given API specifics.
    /// 
    ///     -Global has identical limits for all routes of an API.
    ///     -PerRoute has different limits per API route.
    /// </summary>
    public enum RouteScopeEnum
    {
        Global = 0,
        PerRoute
    }
}
