using System.Net.Http;

namespace WebApiCircuitBreaker.Core.Interfaces
{
    public interface IAddressFinder
    {
        string FindIpAddress(HttpRequestMessage request);
    }
}
