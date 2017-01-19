namespace WebApiCircuitBreaker.Core.Interfaces
{
    public interface ILogger
    {
        void LogCircuitOpen(string message);

        void LogCircuitClosed(string message);

        void LogLowWatermark(string message);

        void LogUnexpectedError(string message);
    }
}
