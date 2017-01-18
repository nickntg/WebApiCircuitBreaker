using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using WebApiCircuitBreaker.Core.Interfaces;

namespace WebApiCircuitBreaker.Extensions.Ip
{
    public class DefaultAddressFinder : IAddressFinder
    {
        public string FindIpAddress(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextBase)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            IEnumerable<string> forwardedFor;
            if (!request.Headers.TryGetValues("X-Forwarded-For", out forwardedFor))
            {
                return string.Empty;
            }

            var str = forwardedFor?.FirstOrDefault();

            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var list = str.Split(',');
            if (list.Length == 0)
            {
                return string.Empty;
            }

            return list.Last().Trim();
        }
    }
}