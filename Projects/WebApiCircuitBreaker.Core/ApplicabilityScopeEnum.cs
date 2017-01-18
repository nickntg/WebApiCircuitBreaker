namespace WebApiCircuitBreaker.Core
{
    /// <summary>
    /// Determines the scope of the circuit breaker given client specifics.
    /// 
    ///     -Global has identical limits regardless of client.
    ///     -PerClient has different limits per remote client.
    /// </summary>
    public enum ApplicabilityScopeEnum
    {
        Global = 0,
        PerClient
    }
}
