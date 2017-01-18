namespace WebApiCircuitBreaker.Core.Interfaces
{
    public interface ILogger
    {
        void LogCircuitOpen(string message);

        void LogCircuitClosed(string message);

        void LogLowWatermark(string message);
    }
}
